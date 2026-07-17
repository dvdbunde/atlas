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

    private OfficerApplicationReviewViewModel _viewModel = new();
    private bool _dataLoaded;
    private bool _isDeciding;

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
        if (_isAssigning || _viewModel?.Application?.ApplicationId == null)
        {
            return;
        }

        _isAssigning = true;
        _viewModel.HasError = false;

        try
        {
            var command = new AssignApplicationToMeCommand { ApplicationId = _viewModel.Application.ApplicationId };
            await Mediator.Send(command);
            await LoadReview();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to assign application {ApplicationId} to current officer", _viewModel.Application?.ApplicationId);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to assign this application. It may already be assigned to another officer.";
        }
        finally
        {
            _isAssigning = false;
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
            Logger.LogError(ex, "Failed to record decision for application {ApplicationId}", _viewModel.Application?.ApplicationId);
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
        if (!await JSRuntime.InvokeAsync<bool>("confirm", "Approve this application?"))
        {
            return;
        }

        await Decide(() => Mediator.Send(new ApproveApplicationCommand
        {
            ApplicationId = _viewModel.Application!.ApplicationId,
            Comments = _viewModel.DecisionComments
        }));
    }

    private async Task Reject()
    {
        if (!await JSRuntime.InvokeAsync<bool>("confirm", "Reject this application? This cannot be undone."))
        {
            return;
        }

        await Decide(() => Mediator.Send(new RejectApplicationCommand
        {
            ApplicationId = _viewModel.Application!.ApplicationId,
            ReasonCode = _viewModel.DecisionReasonCode,
            Comments = _viewModel.DecisionComments
        }));
    }

    private Task RequestInfo() => Decide(() => Mediator.Send(new RequestInfoCommand
    {
        ApplicationId = _viewModel.Application!.ApplicationId,
        Message = _viewModel.DecisionComments
    }));
}