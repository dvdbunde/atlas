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
            _viewModel = OfficerApplicationReviewViewModel.FromDto(result);
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
}