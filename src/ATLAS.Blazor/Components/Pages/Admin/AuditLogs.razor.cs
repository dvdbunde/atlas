using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.AuditLogs;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages.Admin;

public partial class AuditLogs : ComponentBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<AuditLogs> Logger { get; set; } = default!;

    private AuditLogsListViewModel _viewModel = new();

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await LoadLogs();
            StateHasChanged();
        }
    }

    private async Task LoadLogs()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var query = new GetAuditLogsQuery
            {
                SearchTerm = string.IsNullOrWhiteSpace(_viewModel.SearchTerm) ? null : _viewModel.SearchTerm.Trim(),
                Action = string.IsNullOrWhiteSpace(_viewModel.ActionFilter) ? null : _viewModel.ActionFilter.Trim(),
                EntityType = string.IsNullOrWhiteSpace(_viewModel.EntityTypeFilter) ? null : _viewModel.EntityTypeFilter.Trim(),
                SortBy = _viewModel.SortBy,
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
            Logger.LogError(ex, "Failed to load audit logs");
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load audit logs. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    private async Task OnSearchInput(ChangeEventArgs e)
    {
        _viewModel.SearchTerm = e.Value?.ToString() ?? string.Empty;
        _viewModel.PageNumber = 1;
        await LoadLogs();
        StateHasChanged();
    }

    private async Task OnActionFilterChanged(ChangeEventArgs e)
    {
        _viewModel.ActionFilter = e.Value?.ToString();
        _viewModel.PageNumber = 1;
        await LoadLogs();
        StateHasChanged();
    }

    private async Task OnEntityTypeFilterChanged(ChangeEventArgs e)
    {
        _viewModel.EntityTypeFilter = e.Value?.ToString();
        _viewModel.PageNumber = 1;
        await LoadLogs();
        StateHasChanged();
    }

    private async Task OnSortChanged(ChangeEventArgs e)
    {
        var raw = e.Value?.ToString();
        _viewModel.SortBy = Enum.TryParse<AuditLogSortOptionDto>(raw, out var sort)
            ? sort
            : AuditLogSortOptionDto.TimestampDesc;
        _viewModel.PageNumber = 1;
        await LoadLogs();
        StateHasChanged();
    }

    private async Task GoToPage(int page)
    {
        if (page < 1 || page > _viewModel.TotalPages)
            return;
        _viewModel.PageNumber = page;
        await LoadLogs();
        StateHasChanged();
    }
}
