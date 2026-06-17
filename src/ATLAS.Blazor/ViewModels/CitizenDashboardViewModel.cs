using ATLAS.Application.DTOs;
using ATLAS.Domain.Enums;

namespace ATLAS.Blazor.ViewModels;

/// <summary>
/// View model for the Citizen Dashboard page.
/// Encapsulates dashboard state, application summaries, and loading/error states.
/// </summary>
public class CitizenDashboardViewModel
{
    public List<CitizenDashboardCardViewModel> Applications { get; set; } = new();
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsEmpty => !IsLoading && !HasError && Applications.Count == 0;
}

/// <summary>
/// Card-level view model for a single application on the dashboard.
/// </summary>
public class CitizenDashboardCardViewModel
{
    public Guid ApplicationId { get; init; }
    public string ApplicationNumber { get; init; } = string.Empty;
    public string PermitTypeName { get; init; } = string.Empty;
    public ApplicationStatus Status { get; init; }
    public DateTime? SubmittedDate { get; init; }
    public DateTime? LastUpdated { get; init; }
    public string SubmittedDateDisplay => SubmittedDate?.ToString("MMM dd, yyyy") ?? "Not submitted";
    public string LastUpdatedDisplay => LastUpdated?.ToString("MMM dd, yyyy") ?? "N/A";
    public string ActionLabel => Status == ApplicationStatus.Draft ? "Continue Editing" : "View Details";
    public string NavigationUrl => Status == ApplicationStatus.Draft
        ? $"/applications/edit/{ApplicationId}"
        : $"/applications/{ApplicationId}";

    public static CitizenDashboardCardViewModel FromDto(CitizenDashboardDto dto)
    {
        return new CitizenDashboardCardViewModel
        {
            ApplicationId = dto.ApplicationId,
            ApplicationNumber = dto.ApplicationNumber,
            PermitTypeName = dto.PermitTypeName,
            Status = dto.Status,
            SubmittedDate = dto.SubmittedDate,
            LastUpdated = dto.LastUpdated
        };
    }
}