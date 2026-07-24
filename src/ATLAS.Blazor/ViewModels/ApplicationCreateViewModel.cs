using ATLAS.Application.DTOs;

namespace ATLAS.Blazor.ViewModels;

/// <summary>
/// View model for the Application Create page.
/// Manages page state during the draft creation flow.
/// </summary>
public class ApplicationCreateViewModel
{
    public Guid PermitTypeId { get; set; }
    public string PermitName { get; set; } = string.Empty;
    public string PermitDescription { get; set; } = string.Empty;    
    public List<DynamicFormFieldViewModel> Fields { get; set; } = new();
    public bool IsLoading { get; set; } = true;
    public bool IsSaving { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsLoaded => !IsLoading && !HasError;

    public void LoadFromDto(PermitTypeDto dto)
    {
        PermitTypeId = dto.Id;
        PermitName = dto.Name;
        PermitDescription = dto.Description;

        // M8 regression fix: document requirements live in a separate DTO list
        // (PermitTypeDto.DocumentRequirements) and must be merged with the regular
        // fields so FileUpload requirements render on the citizen create/edit form.
        Fields = dto.Fields
            .Concat(dto.DocumentRequirements)
            .Select(DynamicFormFieldViewModel.FromFieldDefinition)
            .ToList();
    }
}