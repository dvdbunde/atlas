namespace ATLAS.Blazor.FormModel;

/// <summary>
/// Defines the display mode for form fields in the DynamicFormGenerator.
/// </summary>
public enum FormFieldMode
{
    /// <summary>
    /// Fields are editable. Users can modify values.
    /// </summary>
    Edit,

    /// <summary>
    /// Fields are read-only. Values are displayed but cannot be modified.
    /// </summary>
    ReadOnly
}
