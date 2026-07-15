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

    public string SubmittedDateDisplay => Application?.SubmittedDate?.ToString("MMM dd, yyyy") ?? "Not submitted";
    public string LastUpdatedDisplay => Application?.LastUpdated?.ToString("MMM dd, yyyy") ?? "N/A";
    public string AssignedOfficerDisplay => Application?.AssignedOfficerName ?? "Unassigned";
    public bool HasReviews => Application?.Reviews.Count > 0;

    public static OfficerApplicationReviewViewModel FromDto(OfficerApplicationReviewDto dto) => new()
    {
        Application = dto
    };
}