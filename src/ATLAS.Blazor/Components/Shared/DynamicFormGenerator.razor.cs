using ATLAS.Blazor.FormModel;
using ATLAS.Blazor.ViewModels;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ATLAS.Blazor.Components.Shared;

public partial class DynamicFormGenerator : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<DynamicFormFieldViewModel> Fields { get; set; } = Array.Empty<DynamicFormFieldViewModel>();

    [Parameter]
    public FormFieldMode Mode { get; set; } = FormFieldMode.Edit;

    [Parameter]
    public EventCallback<DynamicFormFieldViewModel> OnFieldChanged { get; set; }

    [Parameter]
    public EventCallback OnSubmit { get; set; }

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [Parameter]
    public bool ValidateOnInit { get; set; }

    private EditContext _editContext = default!;
    private DynamicFormFieldViewModel[] _orderedFields = Array.Empty<DynamicFormFieldViewModel>();
    private readonly DynamicFormModel _formModel = new();

    protected override void OnParametersSet()
    {
        _editContext = new EditContext(_formModel);

        _orderedFields = Fields
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.FieldName)
            .ToArray();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        if (firstRender && ValidateOnInit && Mode == FormFieldMode.Edit)
        {
            _editContext?.Validate();
        }
    }

    private void HandleValueChanged(DynamicFormFieldViewModel field, string? value)
    {
        field.CurrentValue = value ?? string.Empty;

        var fi = new FieldIdentifier(_formModel, field.FieldName);
        _editContext?.NotifyFieldChanged(fi);

        OnFieldChanged.InvokeAsync(field);
    }

    /// <summary>
    /// Triggers client-side validation for all fields.
    /// Call this from a parent component before saving to ensure validation runs.
    /// </summary>
    public void Validate()
    {
        _editContext?.Validate();
        StateHasChanged();
    }
    
    private async Task HandleSubmit()
    {
        // Trigger validation pipeline so DynamicFieldValidator populates field.Errors
        _editContext?.Validate();

        // Always invoke the parent callback — parent decides whether to proceed
        await OnSubmit.InvokeAsync();
    }

    private void HandleFileSelected(DynamicFormFieldViewModel field, InputFileChangeEventArgs e)
    {
        var file = e.GetMultipleFiles().FirstOrDefault();
        if (file is not null)
        {
            field.SelectedFileName = file.Name;
            field.CurrentValue = file.Name; // Store file name for validation
        }
    }
}
