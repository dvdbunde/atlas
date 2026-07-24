using ATLAS.Application.Queries.Admin;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages.Admin;

public partial class Applications : ComponentBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<Applications> Logger { get; set; } = default!;

    private AdminApplicationExplorerViewModel _viewModel = new();

    private bool _dataLoaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_dataLoaded)
        {
            _dataLoaded = true;
            await LoadPermitTypesAsync();
            await LoadApplications();
            StateHasChanged();
        }
    }

    private async Task LoadPermitTypesAsync()
    {
        try
        {
            var permitTypes = await Mediator.Send(new GetActivePermitTypesQuery());
            _viewModel.PermitTypes = permitTypes
                .Select(p => new PermitTypeFilterOption { Id = p.Id, Name = p.Name })
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Unable to load permit types for filter");
        }
    }

    private async Task LoadApplications(bool showSpinner = true)
    {
        if (showSpinner)
        {
            _viewModel.IsLoading = true;
        }
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var query = new GetAdminApplicationsQuery
            {
                SearchTerm = _viewModel.SearchTerm,
                Status = _viewModel.StatusFilter,
                PermitTypeId = _viewModel.PermitTypeIdFilter,
                DateFrom = _viewModel.DateFrom,
                DateTo = _viewModel.DateTo,
                SortBy = _viewModel.SortBy,
                SortDescending = true,
                PageNumber = _viewModel.PageNumber,
                PageSize = _viewModel.PageSize
            };

            var result = await Mediator.Send(query);
            _viewModel.Items = result.Items;
            _viewModel.TotalCount = result.TotalCount;
            _viewModel.TotalPages = result.TotalPages;
            _viewModel.PageNumber = result.PageNumber;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load applications");
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load applications. Please try again later.";
        }
        finally
        {
            if (showSpinner)
            {
                _viewModel.IsLoading = false;
            }
        }
    }

    private async Task ApplyFilters()
    {
        _viewModel.PageNumber = 1;
        await LoadApplications(showSpinner: false);
        StateHasChanged();
    }

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _viewModel.SearchTerm = e.Value?.ToString() ?? string.Empty;
        await ApplyFilters();
    }

    private async Task OnStatusFilterChanged(ChangeEventArgs e)
    {
        _viewModel.StatusFilter = e.Value?.ToString();
        await ApplyFilters();
    }

    private async Task OnPermitTypeFilterChanged(ChangeEventArgs e)
    {
        var raw = e.Value?.ToString();
        _viewModel.PermitTypeIdFilter = string.IsNullOrWhiteSpace(raw) ? null : Guid.Parse(raw);
        await ApplyFilters();
    }

    private async Task OnDateFromChanged(ChangeEventArgs e)
    {
        var raw = e.Value?.ToString();
        _viewModel.DateFrom = string.IsNullOrWhiteSpace(raw) ? null : DateTime.Parse(raw);
        await ApplyFilters();
    }

    private async Task OnDateToChanged(ChangeEventArgs e)
    {
        var raw = e.Value?.ToString();
        _viewModel.DateTo = string.IsNullOrWhiteSpace(raw) ? null : DateTime.Parse(raw);
        await ApplyFilters();
    }

    private async Task OnSortChanged(ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var sortValue))
            _viewModel.SortBy = (AdminApplicationSortBy)sortValue;
        await ApplyFilters();
    }

    private async Task GoToPage(int page)
    {
        if (page < 1 || page > _viewModel.TotalPages) return;
        _viewModel.PageNumber = page;
        await LoadApplications();
        StateHasChanged();
    }
}