using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace ATLAS.Blazor.Components.Pages;

public partial class PermitSelection : ComponentBase
{
    [Inject]
    private IMediator Mediator { get; set; } = default!;

    [Inject]
    private ILogger<PermitSelection> Logger { get; set; } = default!;

    private PermitSelectionViewModel _viewModel = new();

    private bool _dataLoaded;
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_dataLoaded)
        {
            _dataLoaded = true;
            await LoadPermitTypes();
            StateHasChanged();
        }
    }

    private async Task LoadPermitTypes()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;
        _viewModel.PermitTypes.Clear();

        try
        {
            var query = new GetActivePermitTypesQuery();
            var result = await Mediator.Send(query);

            _viewModel.PermitTypes = result
                .Select(PermitTypeCardViewModel.FromDto)
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load active permit types");
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load permit types. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }
}