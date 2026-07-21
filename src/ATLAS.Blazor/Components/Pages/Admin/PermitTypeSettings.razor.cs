using ATLAS.Application.Commands.PermitTypes;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace ATLAS.Blazor.Components.Pages.Admin;

public partial class PermitTypeSettings : ComponentBase
{
    [Parameter] public string Id { get; set; } = string.Empty;

    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<PermitTypeSettings> Logger { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

    private PermitTypeSettingsViewModel _viewModel = new();

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
            if (result != null)
            {
                _viewModel.Fee = result.Fee;
                _viewModel.IsActive = result.IsActive;
            }
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

    private async Task SaveSettings()
    {
        if (!Guid.TryParse(Id, out var permitTypeId))
            return;

        _viewModel.IsSaving = true;
        _viewModel.SaveMessage = null;
        _viewModel.ErrorMessage = null;

        try
        {
            var command = new UpdatePermitTypeCommand
            {
                PermitTypeId = permitTypeId,
                Fee = _viewModel.Fee,
                IsActive = _viewModel.IsActive
            };

            var result = await Mediator.Send(command);
            if (!result)
            {
                _viewModel.ErrorMessage = "The permit type could not be updated. It may have been removed.";
            }
            else
            {
                _viewModel.SaveMessage = "Settings saved successfully.";
                await LoadPermitType();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update permit type {PermitTypeId}", permitTypeId);
            _viewModel.ErrorMessage = "We were unable to save the settings. Please try again later.";
        }
        finally
        {
            _viewModel.IsSaving = false;
            StateHasChanged();
        }
    }

    private async Task Deactivate()
    {
        if (!Guid.TryParse(Id, out var permitTypeId))
            return;

        _viewModel.IsSaving = true;
        _viewModel.SaveMessage = null;
        _viewModel.ErrorMessage = null;

        try
        {
            var adminId = await GetCurrentAdminId();
            var command = new DeactivatePermitTypeCommand
            {
                PermitTypeId = permitTypeId,
                DeactivatedByAdminId = adminId
            };

            var result = await Mediator.Send(command);
            if (!result)
            {
                _viewModel.ErrorMessage = "The permit type could not be deactivated. It may have been removed.";
            }
            else
            {
                _viewModel.SaveMessage = "Permit type deactivated.";
                await LoadPermitType();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to deactivate permit type {PermitTypeId}", permitTypeId);
            _viewModel.ErrorMessage = "We were unable to deactivate the permit type. Please try again later.";
        }
        finally
        {
            _viewModel.IsSaving = false;
            StateHasChanged();
        }
    }

    private async Task<Guid> GetCurrentAdminId()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var userIdClaim = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
            ?? authState.User.FindFirst("oid")
            ?? authState.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier");

        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var adminId))
            return adminId;

        return Guid.Empty;
    }

    private void BackToDetail()
    {
        if (Guid.TryParse(Id, out var permitTypeId))
            Navigation.NavigateTo($"/admin/permit-types/{permitTypeId}");
    }
}
