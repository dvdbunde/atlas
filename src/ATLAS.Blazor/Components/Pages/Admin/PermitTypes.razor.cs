using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages.Admin;

public partial class PermitTypes : ComponentBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<PermitTypes> Logger { get; set; } = default!;

    private PermitTypesListViewModel _viewModel = new();

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

        try
        {
            var query = new GetPermitTypesQuery
            {
                IncludeInactive = _viewModel.ActiveOnly || _viewModel.InactiveOnly,
                SearchTerm = _viewModel.SearchTerm,
                ActiveOnly = _viewModel.ActiveOnly,
                InactiveOnly = _viewModel.InactiveOnly,
                SortBy = _viewModel.SortBy
            };

            var result = await Mediator.Send(query);
            _viewModel.Items = result.ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load permit types");
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load permit types. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    private async Task ApplyFilters()
    {
        await LoadPermitTypes();
        StateHasChanged();
    }

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _viewModel.SearchTerm = e.Value?.ToString() ?? string.Empty;
        await LoadPermitTypes();
        StateHasChanged();
    }

    private async Task OnActiveOnlyChanged(ChangeEventArgs e)
    {
        _viewModel.ActiveOnly = e.Value is true;
        if (_viewModel.ActiveOnly) _viewModel.InactiveOnly = false;
        await LoadPermitTypes();
        StateHasChanged();
    }

    private async Task OnInactiveOnlyChanged(ChangeEventArgs e)
    {
        _viewModel.InactiveOnly = e.Value is true;
        if (_viewModel.InactiveOnly) _viewModel.ActiveOnly = false;
        await LoadPermitTypes();
        StateHasChanged();
    }

    private async Task OnSortChanged(ChangeEventArgs e)
    {
        _viewModel.SortBy = Enum.TryParse<PermitTypeSortOption>(e.Value?.ToString(), out var sort)
            ? sort
            : PermitTypeSortOption.NameAsc;
        await LoadPermitTypes();
        StateHasChanged();
    }
}
