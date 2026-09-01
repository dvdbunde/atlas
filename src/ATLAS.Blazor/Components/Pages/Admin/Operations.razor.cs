//----------------------
// Operations page code-behind (O5 – Operations Portal)
//----------------------

using ATLAS.Application.Queries.Admin;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages.Admin;

public partial class Operations : ComponentBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<Operations> Logger { get; set; } = default!;

    private OperationsViewModel _viewModel = new();

    private bool _dataLoaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_dataLoaded)
        {
            _dataLoaded = true;
            await LoadOverview();
            StateHasChanged();
        }
    }

    private async Task LoadOverview()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            _viewModel.Overview = await Mediator.Send(new GetOperationsOverviewQuery());
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load operations overview");
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load the operations overview. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }
}