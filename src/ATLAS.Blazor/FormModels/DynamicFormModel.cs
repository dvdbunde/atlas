namespace ATLAS.Blazor.FormModel;

/// <summary>
/// Internal model used by the DynamicFormGenerator as the EditContext root.
/// This is a marker class — FieldIdentifiers are created using this instance
/// combined with the field name, enabling Blazor's validation pipeline to work
/// with dynamic (non-static-property) fields.
/// No data is stored here; actual values live in DynamicFormFieldViewModel.CurrentValue.
/// </summary>
public sealed class DynamicFormModel
{
    // Marker class only — no properties required.
    // FieldIdentifiers reference this instance's Object + field name string.
}
