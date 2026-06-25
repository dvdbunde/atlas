using ATLAS.Application.DTOs;
using ATLAS.Domain.Enums;

namespace ATLAS.Blazor.ViewModels;

/// <summary>
/// UI-focused view model representing a single dynamic form field.
/// Contains display metadata and current input state.
/// No business logic or domain dependencies beyond the FieldType enum.
/// </summary>
public class DynamicFormFieldViewModel
{
    public string FieldName { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public FieldType Type { get; init; }
    public bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public string CurrentValue { get; set; } = string.Empty;
    public int SortOrder { get; init; }
    public List<string> Errors { get; } = new();
    public bool HasErrors => Errors.Count > 0;
    public List<string> Options { get; init; } = new();    
    public string? SelectedFileName { get; set; }
    public string? AllowedExtensions { get; init; }
    public long? MaxFileSizeBytes { get; init; }
    public List<DocumentDto> UploadedDocuments { get; set; } = new();

    public static DynamicFormFieldViewModel FromFieldDefinition(FieldDefinitionDto dto)
    {
        return new DynamicFormFieldViewModel
        {
            FieldName = dto.Name,
            Label = dto.Name,
            Type = dto.Type,
            IsRequired = dto.IsRequired,
            DefaultValue = dto.DefaultValue,
            CurrentValue = dto.DefaultValue ?? string.Empty,
            Options = dto.Options ?? new(),
            SortOrder = 0,
            AllowedExtensions = dto.AllowedExtensions,
            MaxFileSizeBytes = dto.MaxFileSizeBytes
        };
    }

    public static DynamicFormFieldViewModel FromFieldDefinition(
        FieldDefinitionDto dto,
        ApplicationFieldValueViewModel existingValue)
    {
        return new DynamicFormFieldViewModel
        {
            FieldName = dto.Name,
            Label = dto.Name,
            Type = dto.Type,
            IsRequired = dto.IsRequired,
            DefaultValue = dto.DefaultValue,
            CurrentValue = existingValue.Value,
            Options = dto.Options ?? new(),
            SortOrder = existingValue.SortOrder,
            AllowedExtensions = dto.AllowedExtensions,
            MaxFileSizeBytes = dto.MaxFileSizeBytes
        };
    }

    /// <summary>
    /// Creates a FileUpload field from a DocumentRequirementDto.
    /// </summary>
    public static DynamicFormFieldViewModel FromDocumentRequirement(DocumentRequirementDto req)
    {
        return new DynamicFormFieldViewModel
        {
            FieldName = req.DocumentType,
            Label = req.DocumentType,
            Type = FieldType.FileUpload,
            IsRequired = req.IsRequired,
            DefaultValue = null,
            CurrentValue = string.Empty,
            SortOrder = 0,
            AllowedExtensions = req.AllowedExtensions,
            MaxFileSizeBytes = req.MaxFileSizeBytes
        };
    }
}