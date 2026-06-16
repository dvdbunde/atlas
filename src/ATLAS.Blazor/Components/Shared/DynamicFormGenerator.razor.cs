using ATLAS.Blazor.Models;
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

    protected override void OnInitialized()
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
}
