namespace ATLAS.Blazor.ViewModels;

public class PermitTypeCreateViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Fee { get; set; }

    public bool IsSaving { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
}
