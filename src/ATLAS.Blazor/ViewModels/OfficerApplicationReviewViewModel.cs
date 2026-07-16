using ATLAS.Application.DTOs;
using ATLAS.Domain.Enums;

namespace ATLAS.Blazor.ViewModels;

public class OfficerApplicationReviewViewModel
{
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsLoaded => !IsLoading && !HasError && Application != null;
    public OfficerApplicationReviewDto? Application { get; set; }
    public Guid? AssignedOfficerId { get; set; }
    public bool IsAssignedToCurrentOfficer { get; set; }
    public bool CanAssignToMe => !AssignedOfficerId.HasValue;

    public string SubmittedDateDisplay => Application?.SubmittedDate?.ToString("MMM dd, yyyy") ?? "Not submitted";
    public string LastUpdatedDisplay => Application?.LastUpdated?.ToString("MMM dd, yyyy") ?? "N/A";
    public string AssignedOfficerDisplay => Application?.AssignedOfficerName ?? "Unassigned";
    public string AssignmentDisplay => !AssignedOfficerId.HasValue
        ? "Unassigned"
        : IsAssignedToCurrentOfficer ? "Assigned to you"
        : (Application?.AssignedOfficerName ?? "Assigned to another officer");
    public bool HasReviews => Application?.Reviews.Count > 0;

    public static OfficerApplicationReviewViewModel FromDto(OfficerApplicationReviewDto dto, Guid? currentOfficerId) => new()
    {
        Application = dto,
        AssignedOfficerId = dto.AssignedOfficerId,
        IsAssignedToCurrentOfficer = dto.AssignedOfficerId.HasValue
            && dto.AssignedOfficerId == currentOfficerId
    };
}