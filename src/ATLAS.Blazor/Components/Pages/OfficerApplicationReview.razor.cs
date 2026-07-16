using ATLAS.Application.Commands.Applications;
using ATLAS.Application.Interfaces;
using ATLAS.Application.Queries.Applications;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages;

public partial class OfficerApplicationReview : ComponentBase
{
    [Parameter]
    public Guid ApplicationId { get; set; }

    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<OfficerApplicationReview> Logger { get; set; } = default!;
    [Inject] private ICurrentUserService CurrentUserService { get; set; } = default!;

    private bool _isAssigning;

    private OfficerApplicationReviewViewModel _viewModel = new();
    private bool _dataLoaded;

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
}