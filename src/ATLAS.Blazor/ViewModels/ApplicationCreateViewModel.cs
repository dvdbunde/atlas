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
        Fields = dto.Fields
            .Select(DynamicFormFieldViewModel.FromFieldDefinition)
            .ToList();
    }
}