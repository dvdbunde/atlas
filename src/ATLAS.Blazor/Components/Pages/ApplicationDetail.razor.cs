using ATLAS.Application.Queries.Applications;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace ATLAS.Blazor.Components.Pages;

public partial class ApplicationDetail : ComponentBase
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    private IMediator Mediator { get; set; } = default!;

    [Inject]
    private ILogger<ApplicationDetail> Logger { get; set; } = default!;

    private ApplicationDetailViewModel _viewModel = new();

    private bool _dataLoaded;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_dataLoaded)
        {
            _dataLoaded = true;
            await LoadApplication();
            StateHasChanged();
        }
    }    

    private async Task LoadApplication()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var appQuery = new GetApplicationByIdQuery { ApplicationId = Id };
            var application = await Mediator.Send(appQuery);

            if (application is null)
            {
                _viewModel.HasError = true;
                _viewModel.ErrorMessage = "Application not found. It may have been removed or you may not have permission to view it.";
                return;
            }

            var permitQuery = new GetPermitTypeByIdQuery { PermitTypeId = application.PermitTypeId };
            var permitType = await Mediator.Send(permitQuery);

            if (permitType is null)
            {
                _viewModel.HasError = true;
                _viewModel.ErrorMessage = "The permit type for this application is no longer available.";
                return;
            }

            _viewModel.Load(application, permitType);

            await LoadActivities();
        }
        catch (UnauthorizedAccessException)
        {
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "You do not have permission to view this application.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load application {ApplicationId}", Id);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load the application. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    /// <summary>
    /// Maps ReviewDecision to ApplicationStatus for StatusBadge display.
    /// </summary>
    private static ApplicationStatus ReviewDecisionToStatus(ReviewDecision decision) => decision switch
    {
        ReviewDecision.Approve => ApplicationStatus.Approved,
        ReviewDecision.Reject => ApplicationStatus.Rejected,
        ReviewDecision.RequestInfo => ApplicationStatus.InfoRequested,
        _ => ApplicationStatus.UnderReview
    };        

    private async Task LoadActivities()
    {
        try
        {
            var query = new GetApplicationActivityQuery { ApplicationId = _viewModel.ApplicationId };
            var activities = await Mediator.Send(query);
            _viewModel.Activities = activities.ToList();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to load activity for application {ApplicationId}", _viewModel.ApplicationId);
        }
    }
}