using ATLAS.Application.Queries.Applications;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages;

public partial class OfficerDashboard : ComponentBase
{
    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<OfficerDashboard> Logger { get; set; } = default!;

    private OfficerDashboardViewModel _viewModel = new();

    private string SelectedStatusText { get; set; } = "";
    private string SelectedPermitTypeText { get; set; } = "";
    private string SelectedSortText { get; set; } = "LastUpdated";

    private bool _dataLoaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_dataLoaded)
        {
            _dataLoaded = true;
            await LoadPermitTypesAsync();
            await LoadDashboard();
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

    private async Task LoadDashboard()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var query = new GetOfficerDashboardQuery
            {
                Statuses = ParseStatuses(SelectedStatusText),
                PermitTypeId = ParsePermitTypeId(SelectedPermitTypeText),
                SortBy = SelectedSortText == "SubmittedDate"
                    ? OfficerDashboardSortBy.SubmittedDate
                    : OfficerDashboardSortBy.LastUpdated,
                SortDescending = true,
                PageNumber = _viewModel.PageNumber,
                PageSize = _viewModel.PageSize
            };

            var result = await Mediator.Send(query);

            _viewModel.Applications = result.Items
                .Select(OfficerApplicationCardViewModel.FromDto)
                .ToList();
            _viewModel.TotalCount = result.TotalCount;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load officer dashboard");
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load the dashboard. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
            StateHasChanged();
        }
    }

    private async Task OnFilterChanged()
    {
        _viewModel.PageNumber = 1;
        await LoadDashboard();
    }

    private async Task OnSortChanged()
    {
        _viewModel.PageNumber = 1;
        await LoadDashboard();
    }

    private async Task ChangePage(int page)
    {
        if (page < 1 || page > _viewModel.TotalPages) return;
        _viewModel.PageNumber = page;
        await LoadDashboard();
    }

    private static List<ApplicationStatus>? ParseStatuses(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        if (Enum.TryParse<ApplicationStatus>(text.Trim(), out var status))
            return new List<ApplicationStatus> { status };
        return null;
    }

    private static Guid? ParsePermitTypeId(string text)
        => Guid.TryParse(text, out var id) ? id : null;
}