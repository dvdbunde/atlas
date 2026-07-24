using ATLAS.Application.Queries.Applications;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages.Admin;

public partial class ApplicationDetail : ComponentBase
{
    [Parameter]
    public Guid ApplicationId { get; set; }

    [Inject]
    private IMediator Mediator { get; set; } = default!;

    [Inject]
    private ILogger<ApplicationDetail> Logger { get; set; } = default!;

    private AdminApplicationDetailViewModel _viewModel = new();

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
            var appQuery = new GetApplicationByIdQuery { ApplicationId = ApplicationId };
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

            // Fetch citizen email
            try
            {
                var citizenQuery = new ATLAS.Application.Queries.Admin.GetUserByIdQuery { UserId = application.CitizenId };
                var citizen = await Mediator.Send(citizenQuery);
                if (citizen != null)
                {
                    _viewModel.LoadCitizenEmail(citizen.Email);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to load citizen email for application {ApplicationId}", ApplicationId);
            }

            await LoadActivities();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load application {ApplicationId}", ApplicationId);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load the application. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

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

    private static string GetReviewDecisionLabel(ReviewDecision decision) => decision switch
    {
        ReviewDecision.Approve => "Approved",
        ReviewDecision.Reject => "Rejected",
        ReviewDecision.RequestInfo => "Information Requested",
        _ => "Review Recorded"
    };

    private static string GetReviewCardClass(ReviewDecision decision) => decision switch
    {
        ReviewDecision.Approve => "border-success bg-success-subtle",
        ReviewDecision.Reject => "border-danger bg-danger-subtle",
        ReviewDecision.RequestInfo => "border-warning bg-warning-subtle",
        _ => "border-secondary bg-light"
    };
}