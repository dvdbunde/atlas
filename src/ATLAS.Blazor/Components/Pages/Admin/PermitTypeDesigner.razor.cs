using ATLAS.Application.Commands.PermitTypes;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace ATLAS.Blazor.Components.Pages.Admin;

public partial class PermitTypeDesigner : ComponentBase, IAsyncDisposable
{
    [Parameter] public string Id { get; set; } = string.Empty;

    [Inject] private IMediator Mediator { get; set; } = default!;
    [Inject] private ILogger<PermitTypeDesigner> Logger { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = default!;

    private readonly PermitTypeDesignerViewModel _viewModel = new();
    private string _activeSection = "general";
    private bool _dataLoaded;
    private IDisposable? _locationChangingHandler;

    protected override async Task OnInitializedAsync()
    {
        _locationChangingHandler = Navigation.RegisterLocationChangingHandler(OnLocationChanging);
        await LoadPermitType();
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
            var result = await Mediator.Send(new GetPermitTypeByIdQuery { PermitTypeId = permitTypeId });
            _viewModel.PermitType = result;
            _viewModel.NotFound = result == null;
            if (result != null)
            {
                _viewModel.Name = result.Name;
                _viewModel.Description = result.Description;
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

    private void MarkDirty()
    {
        if (!_viewModel.HasUnsavedChanges)
        {
            _viewModel.HasUnsavedChanges = true;
            _ = JSRuntime.InvokeVoidAsync("atlasUnsavedChanges.setDirty", true);
        }
    }

    private void OnNameInput(ChangeEventArgs e)
    {
        _viewModel.Name = e.Value?.ToString() ?? string.Empty;
        MarkDirty();
    }

    private void OnDescriptionInput(ChangeEventArgs e)
    {
        _viewModel.Description = e.Value?.ToString() ?? string.Empty;
        MarkDirty();
    }

    private void SelectSection(string section)
    {
        if (_viewModel.HasUnsavedChanges && section != _activeSection)
        {
            return;
        }

        _activeSection = section;
    }

    private async ValueTask OnLocationChanging(LocationChangingContext context)
    {
        if (_viewModel.HasUnsavedChanges && !context.TargetLocation.Contains("/admin/permit-types", StringComparison.OrdinalIgnoreCase))
        {
            var confirmed = await JSRuntime.InvokeAsync<bool>("window.confirm",
                "You have unsaved changes. Discard them and leave?");
            if (!confirmed)
            {
                context.PreventNavigation();
            }
        }
    }

    private async Task Save()
    {
        if (!Guid.TryParse(Id, out var permitTypeId))
            return;

        _viewModel.IsSaving = true;
        _viewModel.SaveMessage = null;
        _viewModel.ErrorMessage = null;

        try
        {
            var command = new UpdatePermitTypeGeneralInformationCommand
            {
                PermitTypeId = permitTypeId,
                Name = _viewModel.Name,
                Description = _viewModel.Description
            };

            var result = await Mediator.Send(command);
            if (!result)
            {
                _viewModel.ErrorMessage = "The permit type could not be updated. It may have been removed.";
            }
            else
            {
                _viewModel.SaveMessage = "General information saved.";
                _viewModel.HasUnsavedChanges = false;
                await JSRuntime.InvokeVoidAsync("atlasUnsavedChanges.setDirty", false);
                await LoadPermitType();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to update permit type {PermitTypeId}", permitTypeId);
            _viewModel.ErrorMessage = "We were unable to save the changes. Please try again later.";
        }
        finally
        {
            _viewModel.IsSaving = false;
        }
    }

    private void Cancel()
    {
        if (Guid.TryParse(Id, out var permitTypeId))
        {
            _viewModel.HasUnsavedChanges = false;
            _ = JSRuntime.InvokeVoidAsync("atlasUnsavedChanges.setDirty", false);
            Navigation.NavigateTo($"/admin/permit-types/{permitTypeId}");
        }
    }

    private void BackToList()
    {
        Navigation.NavigateTo("/admin/permit-types");
    }

    public async ValueTask DisposeAsync()
    {
        _locationChangingHandler?.Dispose();
        await JSRuntime.InvokeVoidAsync("atlasUnsavedChanges.setDirty", false);
    }
}
