using ATLAS.Application.Queries.Applications;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace ATLAS.Blazor.Components.Pages;

public partial class ConfirmationPage : ComponentBase
{
    [Parameter]
    public Guid Id { get; set; }

    [Inject]
    private IMediator Mediator { get; set; } = default!;

    [Inject]
    private ILogger<ConfirmationPage> Logger { get; set; } = default!;

    private ConfirmationViewModel _viewModel = new();

    private bool _dataLoaded;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_dataLoaded)
        {
            _dataLoaded = true;
            await LoadConfirmation();
            StateHasChanged();
        }
    }    

    private async Task LoadConfirmation()
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
                _viewModel.ErrorMessage = "Application not found.";
                return;
            }

            var permitQuery = new GetPermitTypeByIdQuery { PermitTypeId = application.PermitTypeId };
            var permitType = await Mediator.Send(permitQuery);

            _viewModel.Load(application, permitType ?? new());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load confirmation for application {ApplicationId}", Id);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load the confirmation details. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }
}