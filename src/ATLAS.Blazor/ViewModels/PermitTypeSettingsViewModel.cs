using ATLAS.Application.DTOs;

namespace ATLAS.Blazor.ViewModels;

public class PermitTypeSettingsViewModel
{
    public PermitTypeDto? PermitType { get; set; }
    public decimal Fee { get; set; }
    public bool IsActive { get; set; }

    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public bool NotFound { get; set; }
    public bool IsSaving { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SaveMessage { get; set; }
}
