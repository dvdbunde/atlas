using ATLAS.Application.Queries.Admin;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages.Admin;

public partial class AdminDashboard : ComponentBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<AdminDashboard> Logger { get; set; } = default!;

    private AdminDashboardViewModel _viewModel = new();

    private bool _dataLoaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_dataLoaded)
        {
            _dataLoaded = true;
            await LoadDashboard();
            StateHasChanged();
        }
    }

    private async Task LoadDashboard()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var query = new GetAdminDashboardQuery();
            _viewModel.Summary = await Mediator.Send(query);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load administration dashboard");
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load the dashboard. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }
}
