using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Applications;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Components;

namespace ATLAS.Blazor.Components.Pages;

public partial class CitizenDashboard : ComponentBase
{
    [Inject]
    private IMediator Mediator { get; set; } = default!;

    [Inject]
    private ILogger<CitizenDashboard> Logger { get; set; } = default!;

    private CitizenDashboardViewModel _viewModel = new();

    private bool _dataLoaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_dataLoaded)
        {
            _dataLoaded = true;
            await LoadPermitTypes();
            await LoadDashboard();
            StateHasChanged();
        }
    }

    private async Task LoadPermitTypes()
    {
        try
        {
            var permitTypes = await Mediator.Send(new GetPermitTypesQuery
            {
                StatusFilter = PermitTypeStatusFilter.Active
            });
            _viewModel.PermitTypes = permitTypes.ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load permit types for citizen dashboard");
            _viewModel.PermitTypes = new List<PermitTypeSummaryDto>();
        }
    }

    private async Task LoadDashboard()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var query = new GetCitizenDashboardQuery
            {
                PermitTypeId = _viewModel.PermitTypeIdFilter,
                Status = _viewModel.StatusFilter,
                SortBy = _viewModel.SortBy,
                SortDescending = true
            };
            var result = await Mediator.Send(query);

            _viewModel.Applications = result
                .Select(CitizenDashboardCardViewModel.FromDto)
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load citizen dashboard");
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load your applications. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    private async Task OnPermitTypeFilterChanged(ChangeEventArgs e)
    {
        var raw = e.Value?.ToString();
        _viewModel.PermitTypeIdFilter = Guid.TryParse(raw, out var id) ? id : null;
        await LoadDashboard();
        StateHasChanged();
    }

    private async Task OnStatusFilterChanged(ChangeEventArgs e)
    {
        var raw = e.Value?.ToString();
        _viewModel.StatusFilter = string.IsNullOrWhiteSpace(raw)
            ? null
            : Enum.TryParse<ApplicationStatus>(raw, out var status) ? status : null;
        await LoadDashboard();
        StateHasChanged();
    }

    private async Task OnSortChanged(ChangeEventArgs e)
    {
        var raw = e.Value?.ToString();
        _viewModel.SortBy = Enum.TryParse<CitizenDashboardSortBy>(raw, out var sort)
            ? sort
            : CitizenDashboardSortBy.LastUpdated;
        await LoadDashboard();
        StateHasChanged();
    }
}