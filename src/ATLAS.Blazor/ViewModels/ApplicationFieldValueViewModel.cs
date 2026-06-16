using ATLAS.Domain.Entities;

namespace ATLAS.Blazor.ViewModels;

/// <summary>
/// UI-focused view model representing a single field value for an application.
/// Used to pre-populate the DynamicFormGenerator when editing an existing draft.
/// No business logic or domain dependencies.
/// </summary>
public class ApplicationFieldValueViewModel
{
    public string FieldName { get; init; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SortOrder { get; init; }

    public static ApplicationFieldValueViewModel FromEntity(ApplicationFieldValue entity)
    {
        return new ApplicationFieldValueViewModel
        {
            FieldName = entity.FieldName,
            Value = entity.Value,
            SortOrder = entity.SortOrder
        };
    }
}
