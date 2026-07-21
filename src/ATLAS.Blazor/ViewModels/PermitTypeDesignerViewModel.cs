using ATLAS.Application.DTOs;

namespace ATLAS.Blazor.ViewModels;

public class PermitTypeDesignerViewModel
{
    public PermitTypeDto? PermitType { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public bool NotFound { get; set; }
    public bool IsSaving { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SaveMessage { get; set; }

    public bool HasUnsavedChanges { get; set; }
}
