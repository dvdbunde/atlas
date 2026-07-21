using ATLAS.Application.DTOs;

namespace ATLAS.Blazor.ViewModels;

public class PermitTypeDetailViewModel
{
    public PermitTypeDto? PermitType { get; set; }
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public bool NotFound { get; set; }
    public string? ErrorMessage { get; set; }
}
