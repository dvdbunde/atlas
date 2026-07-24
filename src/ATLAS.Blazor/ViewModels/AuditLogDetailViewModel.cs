using ATLAS.Application.DTOs;

namespace ATLAS.Blazor.ViewModels;

public class AuditLogDetailViewModel
{
    public AuditLogDto? AuditLog { get; set; }
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public bool NotFound { get; set; }
    public string? ErrorMessage { get; set; }
}
