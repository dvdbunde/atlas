using ATLAS.Application.Queries.Admin;

namespace ATLAS.Blazor.ViewModels;

/// <summary>
/// View model for the Administration Dashboard page.
/// Encapsulates dashboard summary state and loading/error states.
/// </summary>
public class AdminDashboardViewModel
{
    public AdminDashboardDto? Summary { get; set; }
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsEmpty => !IsLoading && !HasError && Summary == null;
}
