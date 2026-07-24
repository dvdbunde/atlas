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
    public string DecisionComments { get; set; } = string.Empty;
    public string DecisionReasonCode { get; set; } = string.Empty;
    // True only when assigned to current officer AND status allows a decision.
    public bool CanDecide => IsAssignedToCurrentOfficer
        && Application?.Status == ApplicationStatus.UnderReview;
    public List<ApplicationActivityDto> Activities { get; set; } = new();

    // Mapped properties for shared layout consumption
    public List<FieldDisplayViewModel> Fields => Application?.FieldValues
        .Select(fv => new FieldDisplayViewModel
        {
            Label = fv.Label,
            Value = fv.Value
        })
        .ToList() ?? new();

    public List<DocumentDto> FlatDocuments => Application?.DocumentRequirements
        .SelectMany(dr => dr.UploadedDocuments)
        .Select(d => new DocumentDto
        {
            Id = d.Id,
            FileName = d.FileName,
            ContentType = d.ContentType,
            FileSize = d.FileSize,
            UploadedDate = d.UploadedDate
        })
        .ToList() ?? new();

    public List<ReviewDisplayViewModel> MappedReviews => Application?.Reviews
        .Select(r => new ReviewDisplayViewModel
        {
            Decision = r.Decision,
            Comments = r.Comments ?? string.Empty,
            ReviewedDate = r.ReviewedDate,
            ReasonCode = r.ReasonCode
        })
        .ToList() ?? new();

    public static OfficerApplicationReviewViewModel FromDto(OfficerApplicationReviewDto dto, Guid? currentOfficerId) => new()
    {
        Application = dto,
        AssignedOfficerId = dto.AssignedOfficerId,
        IsAssignedToCurrentOfficer = dto.AssignedOfficerId.HasValue
            && dto.AssignedOfficerId == currentOfficerId
    };
}