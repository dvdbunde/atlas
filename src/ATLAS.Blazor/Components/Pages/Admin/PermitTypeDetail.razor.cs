using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages.Admin;

public partial class PermitTypeDetail : ComponentBase
{
    [Parameter] public string Id { get; set; } = string.Empty;

    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<PermitTypeDetail> Logger { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;

    private PermitTypeDetailViewModel _viewModel = new();

    private bool _dataLoaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !_dataLoaded)
        {
            _dataLoaded = true;
            await LoadPermitType();
            StateHasChanged();
        }
    }

    private async Task LoadPermitType()
    {
        _viewModel.IsLoading = true;
        _viewModel.HasError = false;
        _viewModel.NotFound = false;
        _viewModel.ErrorMessage = null;

        if (!Guid.TryParse(Id, out var permitTypeId))
        {
            _viewModel.IsLoading = false;
            _viewModel.NotFound = true;
            return;
        }

        try
        {
            var query = new GetPermitTypeByIdQuery { PermitTypeId = permitTypeId };
            var result = await Mediator.Send(query);
            _viewModel.PermitType = result;
            _viewModel.NotFound = result == null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to load permit type {PermitTypeId}", permitTypeId);
            _viewModel.HasError = true;
            _viewModel.ErrorMessage = "We were unable to load this permit type. Please try again later.";
        }
        finally
        {
            _viewModel.IsLoading = false;
        }
    }

    private void GoToSettings()
    {
        if (Guid.TryParse(Id, out var permitTypeId))
        {
            Navigation.NavigateTo($"/admin/permit-types/{permitTypeId}/settings");
        }
    }

    private void BackToList()
    {
        Navigation.NavigateTo("/admin/permit-types");
    }
}
