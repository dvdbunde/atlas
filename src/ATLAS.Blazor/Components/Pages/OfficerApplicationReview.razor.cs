using ATLAS.Application.Commands.Applications;
using ATLAS.Application.Interfaces;
using ATLAS.Application.Queries.Applications;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace ATLAS.Blazor.Components.Pages;

public partial class OfficerApplicationReview : ComponentBase
{
    [Parameter]
    public Guid ApplicationId { get; set; }

    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<OfficerApplicationReview> Logger { get; set; } = default!;
    [Inject] private ICurrentUserService CurrentUserService { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private bool _isAssigning;
    private bool _isReleasing;

    private OfficerApplicationReviewViewModel _viewModel = new();
    private bool _dataLoaded;
    private bool _isDeciding;

    // Pending decision action awaiting inline confirmation inside the Officer Decision block.
    private PendingDecisionAction? _pendingAction;

    private enum PendingDecisionAction
    {
        Approve,
        Reject,
        RequestInfo
    }

    private bool IsConfirming => _pendingAction.HasValue;

    private string ConfirmTitle => _pendingAction switch
    {
        PendingDecisionAction.Approve => "Approve application",
        PendingDecisionAction.Reject => "Reject application",
        PendingDecisionAction.RequestInfo => "Request additional information",
        _ => string.Empty
    };

    private string ConfirmMessage => _pendingAction switch
    {
        PendingDecisionAction.Approve => $"Approve application {_viewModel.Application?.ApplicationNumber ?? ""}? This will mark the application as approved.",
        PendingDecisionAction.Reject => $"Reject application {_viewModel.Application?.ApplicationNumber ?? ""}? This is a workflow decision and may notify the citizen.",
        PendingDecisionAction.RequestInfo => $"Request additional information for application {_viewModel.Application?.ApplicationNumber ?? ""}? The citizen will be asked to provide more details.",
        _ => string.Empty
    };

    private string ConfirmLabel => _pendingAction switch
    {
        PendingDecisionAction.Approve => "Approve",
        PendingDecisionAction.Reject => "Reject",
        PendingDecisionAction.RequestInfo => "Request Information",
        _ => string.Empty
    };

    private string ConfirmButtonClass => _pendingAction switch
    {
        PendingDecisionAction.Approve => "btn btn-success",
        PendingDecisionAction.Reject => "btn btn-danger",
        PendingDecisionAction.RequestInfo => "btn btn-warning",
        _ => "btn btn-primary"
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_dataLoaded)
        {
            _dataLoaded = true;
            await LoadReview();
            StateHasChanged();
        }
    }

    private async Task LoadReview()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var result = await Mediator.Send(new GetOfficerApplicationReviewQuery { ApplicationId = ApplicationId });
            if (result is null)
            {
                _viewModel.HasError = true;
                _viewModel.ErrorMessage = "Application not found.";
                return;
            }
            _viewModel = OfficerApplicationReviewViewModel.FromDto(result, CurrentUserService.UserId);

            // Load activity feed
            try
            {
                var activityQuery = new GetApplicationActivityQuery { ApplicationId = ApplicationId };
                var activities = await Mediator.Send(activityQuery);
                _viewModel.Activities = activities.ToList();
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to load activity for application {ApplicationId}", ApplicationId);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load officer review {ApplicationId}", ApplicationId);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load the application. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    private static ApplicationStatus ReviewDecisionToStatus(ReviewDecision decision) => decision switch
    {
        ReviewDecision.Approve => ApplicationStatus.Approved,
        ReviewDecision.Reject => ApplicationStatus.Rejected,
        ReviewDecision.RequestInfo => ApplicationStatus.InfoRequested,
        _ => ApplicationStatus.UnderReview
    };

    private async Task AssignToMe()
    {
        if (_isAssigning || _viewModel?.Application?.Id == Guid.Empty)
        {
            return;
        }

        _isAssigning = true;
        _viewModel.HasError = false;

        try
        {
            var command = new AssignApplicationToMeCommand { ApplicationId = _viewModel.Application.Id };
            await Mediator.Send(command);
            await LoadReview();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to assign application {ApplicationId} to current officer", _viewModel.Application?.Id);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to assign this application. It may already be assigned to another officer.";
        }
        finally
        {
            _isAssigning = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task ReleaseAssignment()
    {
        if (_isReleasing || _viewModel?.Application?.Id == Guid.Empty)
        {
            return;
        }

        _isReleasing = true;
        _viewModel.HasError = false;

        try
        {
            var command = new ReleaseApplicationCommand { ApplicationId = _viewModel.Application.Id };
            await Mediator.Send(command);
            await LoadReview();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to release application {ApplicationId}", _viewModel.Application?.Id);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to release this application. It may no longer be assigned to you or is in an invalid state.";
        }
        finally
        {
            _isReleasing = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task Decide(Func<Task> action)
    {
        if (_isDeciding)
            return;

        _isDeciding = true;
        _viewModel.HasError = false;

        try
        {
            await action();
            await LoadReview();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to record decision for application {ApplicationId}", _viewModel.Application?.Id);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to record the decision. The application may not be assigned to you or is in an invalid state.";
        }
        finally
        {
            _isDeciding = false;
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task Approve()
    {
        // Comments are optional for approval; no rejection reason is applicable.
        _viewModel.DecisionReasonCode = string.Empty;
        _viewModel.ClearValidationErrors();
        _pendingAction = PendingDecisionAction.Approve;
    }

    private async Task Reject()
    {
        // Comments and a valid rejection reason code are mandatory for rejection.
        _viewModel.ClearValidationErrors();
        if (!ValidateReject())
        {
            return;
        }

        _pendingAction = PendingDecisionAction.Reject;
    }

    private async Task RequestInfo()
    {
        // Comments are mandatory for requesting information; no rejection reason is applicable.
        _viewModel.DecisionReasonCode = string.Empty;
        _viewModel.ClearValidationErrors();
        if (!ValidateRequestInfo())
        {
            return;
        }

        _pendingAction = PendingDecisionAction.RequestInfo;
    }

    private bool ValidateReject()
    {
        var valid = true;

        if (string.IsNullOrWhiteSpace(_viewModel.DecisionComments))
        {
            _viewModel.CommentsError = "Comments / Instructions are required when rejecting an application.";
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(_viewModel.DecisionReasonCode))
        {
            _viewModel.ReasonCodeError = "Rejection Reason Code is required when rejecting an application.";
            valid = false;
        }

        return valid;
    }

    private bool ValidateRequestInfo()
    {
        if (string.IsNullOrWhiteSpace(_viewModel.DecisionComments))
        {
            _viewModel.CommentsError = "Comments / Instructions are required when requesting additional information.";
            return false;
        }

        return true;
    }

    private void OnConfirmCancel()
    {
        _pendingAction = null;
    }

    private async Task OnConfirmConfirm()
    {
        var action = _pendingAction;
        _pendingAction = null;

        switch (action)
        {
            case PendingDecisionAction.Approve:
                await Decide(() => Mediator.Send(new ApproveApplicationCommand
                {
                    ApplicationId = _viewModel.Application!.Id,
                    Comments = _viewModel.DecisionComments
                }));
                break;
            case PendingDecisionAction.Reject:
                await Decide(() => Mediator.Send(new RejectApplicationCommand
                {
                    ApplicationId = _viewModel.Application!.Id,
                    ReasonCode = _viewModel.DecisionReasonCode,
                    Comments = _viewModel.DecisionComments
                }));
                break;
            case PendingDecisionAction.RequestInfo:
                await Decide(() => Mediator.Send(new RequestInfoCommand
                {
                    ApplicationId = _viewModel.Application!.Id,
                    Message = _viewModel.DecisionComments
                }));
                break;
            default:
                break;
        }
    }
}