using ATLAS.Application.Queries.Applications;
using ATLAS.Blazor.ViewModels;
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

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboard();
    }

    private async Task LoadDashboard()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.ErrorMessage = null;

        try
        {
            var query = new GetCitizenDashboardQuery();
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
}