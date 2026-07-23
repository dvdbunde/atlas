using ATLAS.Application.Commands.PermitTypes;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Enums;
using ATLAS.Blazor.FormModel;
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
    private IDisposable? _locationChangingHandler;
    private bool _hasRenderedInteractively;

    private IReadOnlyList<DynamicFormFieldViewModel> _previewFields =>
        _viewModel.Fields.Concat(_viewModel.DocumentRequirements)
            .Select(DynamicFormFieldViewModel.FromFieldDefinition)
            .ToList();

    protected override async Task OnInitializedAsync()
    {
        _locationChangingHandler = Navigation.RegisterLocationChangingHandler(OnLocationChanging);
        await LoadPermitType();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            _hasRenderedInteractively = true;
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
            var result = await Mediator.Send(new GetPermitTypeByIdQuery { PermitTypeId = permitTypeId });
            _viewModel.PermitType = result;
            _viewModel.NotFound = result == null;
            if (result != null)
            {
                _viewModel.Name = result.Name;
                _viewModel.Description = result.Description;
                _viewModel.Fields = result.Fields;
                _viewModel.DocumentRequirements = result.DocumentRequirements;
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

    private bool _suppressUnsavedGuard;

    private async ValueTask OnLocationChanging(LocationChangingContext context)
    {
        if (_suppressUnsavedGuard)
            return;

        if (_viewModel.HasUnsavedChanges)
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
                _suppressUnsavedGuard = true;
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
            _suppressUnsavedGuard = true;
            _ = JSRuntime.InvokeVoidAsync("atlasUnsavedChanges.setDirty", false);
            Navigation.NavigateTo($"/admin/permit-types/{permitTypeId}");
        }
    }

    private void BackToList()
    {
        _suppressUnsavedGuard = true;
        Navigation.NavigateTo("/admin/permit-types");
    }

    #region Field editing

    private Guid? _editingFieldId;
    private readonly FieldEditorModel _fieldDraft = new();
    private bool _isFieldEditorOpen;

    private void OpenAddField()
    {
        if (_viewModel.HasUnsavedChanges)
            return;
        _editingFieldId = null;
        _fieldDraft.Reset();
        _isFieldEditorOpen = true;
        MarkDirty();
    }

    private void OpenEditField(FieldDefinitionDto field)
    {
        if (_viewModel.HasUnsavedChanges)
            return;
        _editingFieldId = field.Id;
        _fieldDraft.Name = field.Name;
        _fieldDraft.Type = field.Type;
        _fieldDraft.IsRequired = field.IsRequired;
        _fieldDraft.DefaultValue = field.DefaultValue ?? string.Empty;
        _fieldDraft.OptionsText = field.Options != null ? string.Join(Environment.NewLine, field.Options) : string.Empty;
        _isFieldEditorOpen = true;
        MarkDirty();
    }

    private void CloseFieldEditor(bool discard = true)
    {
        _isFieldEditorOpen = false;
        _editingFieldId = null;
        _fieldDraft.Reset();
        if (discard)
        {
            _viewModel.HasUnsavedChanges = false;
            _ = JSRuntime.InvokeVoidAsync("atlasUnsavedChanges.setDirty", false);
        }
    }

    private async Task SaveField()
    {
        if (!Guid.TryParse(Id, out var permitTypeId))
            return;

        var options = _fieldDraft.Type == FieldType.Dropdown
            ? _fieldDraft.OptionsText.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : null;

        try
        {
            bool result;
            if (_editingFieldId.HasValue)
            {
                result = await Mediator.Send(new UpdatePermitFieldCommand
                {
                    PermitTypeId = permitTypeId,
                    FieldId = _editingFieldId.Value,
                    Name = _fieldDraft.Name,
                    Type = _fieldDraft.Type,
                    IsRequired = _fieldDraft.IsRequired,
                    DefaultValue = string.IsNullOrWhiteSpace(_fieldDraft.DefaultValue) ? null : _fieldDraft.DefaultValue,
                    Options = options
                });
            }
            else
            {
                result = await Mediator.Send(new AddPermitFieldCommand
                {
                    PermitTypeId = permitTypeId,
                    Name = _fieldDraft.Name,
                    Type = _fieldDraft.Type,
                    IsRequired = _fieldDraft.IsRequired,
                    DefaultValue = string.IsNullOrWhiteSpace(_fieldDraft.DefaultValue) ? null : _fieldDraft.DefaultValue,
                    Options = options
                });
            }

            if (!result)
            {
                _viewModel.ErrorMessage = "The field could not be saved. The permit type may have been removed.";
                return;
            }

            _isFieldEditorOpen = false;
            _editingFieldId = null;
            _fieldDraft.Reset();
            _viewModel.HasUnsavedChanges = false;
            await JSRuntime.InvokeVoidAsync("atlasUnsavedChanges.setDirty", false);
            _viewModel.SaveMessage = "Field saved.";
            await LoadPermitType();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save field for permit type {PermitTypeId}", permitTypeId);
            _viewModel.ErrorMessage = "We were unable to save the field. Please try again later.";
        }
    }

    private async Task RemoveField(FieldDefinitionDto field)
    {
        if (!Guid.TryParse(Id, out var permitTypeId))
            return;

        try
        {
            var result = await Mediator.Send(new RemovePermitFieldCommand
            {
                PermitTypeId = permitTypeId,
                FieldId = field.Id
            });
            if (result)
            {
                _viewModel.SaveMessage = "Field removed.";
                await LoadPermitType();
            }
            else
            {
                _viewModel.ErrorMessage = "The field could not be removed.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove field for permit type {PermitTypeId}", permitTypeId);
            _viewModel.ErrorMessage = "We were unable to remove the field. Please try again later.";
        }
    }

    private async Task MoveField(FieldDefinitionDto field, int direction)
    {
        if (!Guid.TryParse(Id, out var permitTypeId))
            return;

        var currentOrder = _viewModel.Fields.IndexOf(field) + 1;
        var newOrder = currentOrder + direction;
        if (newOrder < 1 || newOrder > _viewModel.Fields.Count)
            return;

        try
        {
            var result = await Mediator.Send(new MovePermitFieldCommand
            {
                PermitTypeId = permitTypeId,
                FieldId = field.Id,
                NewOrder = newOrder
            });
            if (result)
            {
                await LoadPermitType();
            }
            else
            {
                _viewModel.ErrorMessage = "The field could not be reordered.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to reorder field for permit type {PermitTypeId}", permitTypeId);
            _viewModel.ErrorMessage = "We were unable to reorder the field. Please try again later.";
        }
    }

    #endregion

    #region Document requirement editing

    private Guid? _editingRequirementId;
    private readonly DocumentRequirementEditorModel _requirementDraft = new();
    private bool _isRequirementEditorOpen;

    private void OpenAddRequirement()
    {
        if (_viewModel.HasUnsavedChanges)
            return;
        _editingRequirementId = null;
        _requirementDraft.Reset();
        _isRequirementEditorOpen = true;
        MarkDirty();
    }

    private void OpenEditRequirement(FieldDefinitionDto requirement)
    {
        if (_viewModel.HasUnsavedChanges)
            return;
        _editingRequirementId = requirement.Id;
        _requirementDraft.DocumentType = requirement.Name;
        _requirementDraft.IsRequired = requirement.IsRequired;
        _requirementDraft.AllowedExtensionsText = requirement.AllowedExtensions ?? string.Empty;
        _requirementDraft.MaxFileSizeBytes = requirement.MaxFileSizeBytes ?? 0;
        _isRequirementEditorOpen = true;
        MarkDirty();
    }

    private void CloseRequirementEditor(bool discard = true)
    {
        _isRequirementEditorOpen = false;
        _editingRequirementId = null;
        _requirementDraft.Reset();
        if (discard)
        {
            _viewModel.HasUnsavedChanges = false;
            _ = JSRuntime.InvokeVoidAsync("atlasUnsavedChanges.setDirty", false);
        }
    }

    private async Task SaveRequirement()
    {
        if (!Guid.TryParse(Id, out var permitTypeId))
            return;

        var extensions = _requirementDraft.AllowedExtensionsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        try
        {
            bool result;
            if (_editingRequirementId.HasValue)
            {
                result = await Mediator.Send(new UpdateDocumentRequirementCommand
                {
                    PermitTypeId = permitTypeId,
                    RequirementId = _editingRequirementId.Value,
                    IsRequired = _requirementDraft.IsRequired,
                    AllowedExtensions = extensions,
                    MaxFileSizeBytes = _requirementDraft.MaxFileSizeBytes
                });
            }
            else
            {
                result = await Mediator.Send(new AddDocumentRequirementCommand
                {
                    PermitTypeId = permitTypeId,
                    DocumentType = _requirementDraft.DocumentType,
                    IsRequired = _requirementDraft.IsRequired,
                    AllowedExtensions = extensions,
                    MaxFileSizeBytes = _requirementDraft.MaxFileSizeBytes
                });
            }

            if (!result)
            {
                _viewModel.ErrorMessage = "The document requirement could not be saved. The permit type may have been removed.";
                return;
            }

            _isRequirementEditorOpen = false;
            _editingRequirementId = null;
            _requirementDraft.Reset();
            _viewModel.HasUnsavedChanges = false;
            await JSRuntime.InvokeVoidAsync("atlasUnsavedChanges.setDirty", false);
            _viewModel.SaveMessage = "Document requirement saved.";
            await LoadPermitType();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to save document requirement for permit type {PermitTypeId}", permitTypeId);
            _viewModel.ErrorMessage = "We were unable to save the document requirement. Please try again later.";
        }
    }

    private async Task RemoveRequirement(FieldDefinitionDto requirement)
    {
        if (!Guid.TryParse(Id, out var permitTypeId))
            return;

        try
        {
            var result = await Mediator.Send(new RemoveDocumentRequirementCommand
            {
                PermitTypeId = permitTypeId,
                RequirementId = requirement.Id
            });
            if (result)
            {
                _viewModel.SaveMessage = "Document requirement removed.";
                await LoadPermitType();
            }
            else
            {
                _viewModel.ErrorMessage = "The document requirement could not be removed.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to remove document requirement for permit type {PermitTypeId}", permitTypeId);
            _viewModel.ErrorMessage = "We were unable to remove the document requirement. Please try again later.";
        }
    }

    private async Task MoveRequirement(FieldDefinitionDto requirement, int direction)
    {
        if (!Guid.TryParse(Id, out var permitTypeId))
            return;

        var currentOrder = _viewModel.DocumentRequirements.IndexOf(requirement) + 1;
        var newOrder = currentOrder + direction;
        if (newOrder < 1 || newOrder > _viewModel.DocumentRequirements.Count)
            return;

        try
        {
            var result = await Mediator.Send(new MoveDocumentRequirementCommand
            {
                PermitTypeId = permitTypeId,
                RequirementId = requirement.Id,
                NewOrder = newOrder
            });
            if (result)
            {
                await LoadPermitType();
            }
            else
            {
                _viewModel.ErrorMessage = "The document requirement could not be reordered.";
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to reorder document requirement for permit type {PermitTypeId}", permitTypeId);
            _viewModel.ErrorMessage = "We were unable to reorder the document requirement. Please try again later.";
        }
    }

    #endregion

    public async ValueTask DisposeAsync()
    {
        _locationChangingHandler?.Dispose();

        // JS interop is only available after the component has rendered interactively.
        // During prerendering/static disposal, skip the call to avoid InvalidOperationException.
        if (_hasRenderedInteractively)
        {
            await JSRuntime.InvokeVoidAsync("atlasUnsavedChanges.setDirty", false);
        }
    }
}

public class FieldEditorModel
{
    public string Name { get; set; } = string.Empty;
    public FieldType Type { get; set; } = FieldType.Text;
    public bool IsRequired { get; set; }
    public string DefaultValue { get; set; } = string.Empty;
    public string OptionsText { get; set; } = string.Empty;

    public void Reset()
    {
        Name = string.Empty;
        Type = FieldType.Text;
        IsRequired = false;
        DefaultValue = string.Empty;
        OptionsText = string.Empty;
    }
}

public class DocumentRequirementEditorModel
{
    public string DocumentType { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string AllowedExtensionsText { get; set; } = string.Empty;
    public long MaxFileSizeBytes { get; set; }

    public void Reset()
    {
        DocumentType = string.Empty;
        IsRequired = false;
        AllowedExtensionsText = string.Empty;
        MaxFileSizeBytes = 0;
    }
}
