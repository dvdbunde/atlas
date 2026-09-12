using ATLAS.Application.DTOs;
using ATLAS.Domain.Enums;

namespace ATLAS.Blazor.ViewModels;

public class OfficerApplicationReviewViewModel
{
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsLoaded => !IsLoading && !HasError && Dto?.Application != null;
    public OfficerApplicationReviewDto? Dto { get; set; }
    public ApplicationDetailDto? Application => Dto?.Application;
    public Guid? AssignedOfficerId => Application?.AssignedOfficerId;
    public bool IsAssignedToCurrentOfficer { get; set; }
    public bool CanAssignToMe => !AssignedOfficerId.HasValue;
    // True only when assigned to current officer AND status is Under Review.
    // Self-unassignment is a workflow/assignment action, distinct from decisions.
    public bool CanReleaseAssignment => IsAssignedToCurrentOfficer
        && Application?.Status == ApplicationStatus.UnderReview;

    public string SubmittedDateDisplay => Application?.SubmittedDate?.ToString("dd/MM/yyyy HH:mm") ?? "Not submitted";
    public string LastUpdatedDisplay => (Application?.ReviewedDate ?? Application?.SubmittedDate)?.ToString("dd/MM/yyyy HH:mm") ?? "N/A";
    public string AssignedOfficerDisplay => Application?.AssignedOfficerName ?? "Unassigned";
    public string AssignmentDisplay => !AssignedOfficerId.HasValue
        ? "Unassigned"
        : IsAssignedToCurrentOfficer ? "Assigned to you"
        : (Application?.AssignedOfficerName ?? "Assigned to another officer");
    public bool HasReviews => Application?.Reviews.Count > 0;
    public string DecisionComments { get; set; } = string.Empty;
    public string DecisionReasonCode { get; set; } = string.Empty;
    public string? CommentsError { get; set; }
    public string? ReasonCodeError { get; set; }
    // True only when assigned to current officer AND status allows a decision.
    public bool CanDecide => IsAssignedToCurrentOfficer
        && Application?.Status == ApplicationStatus.UnderReview;

    public void ClearValidationErrors()
    {
        CommentsError = null;
        ReasonCodeError = null;
    }
    public List<ApplicationActivityDto> Activities { get; set; } = new();

    // Mapped properties for shared layout consumption.
    // Only genuine application fields are shown under "Submitted Application Data".
    // Document requirements (e.g. "Proof of Address") are stored as field values too,
    // so exclude any value whose name matches a permit-type document requirement.
    public List<FieldDisplayViewModel> Fields => Application?.FieldValues
        .Where(fv => !DocumentRequirements.Any(dr =>
            dr.DocumentType.Equals(fv.Key, StringComparison.OrdinalIgnoreCase)))
        .Select(fv => new FieldDisplayViewModel
        {
            Label = fv.Key,
            Value = fv.Value
        })
        .ToList() ?? new();

    public List<DocumentDto> FlatDocuments => Application?.Documents.ToList() ?? new();

    public List<ReviewDisplayViewModel> MappedReviews => Application?.Reviews
        .Select(r => new ReviewDisplayViewModel
        {
            Decision = r.Decision,
            Comments = r.Comments ?? string.Empty,
            ReviewedDate = r.ReviewedDate,
            ReasonCode = r.ReasonCode
        })
        .ToList() ?? new();

    public List<OfficerDocumentRequirementDto> DocumentRequirements => Dto?.DocumentRequirements ?? new();

    public static OfficerApplicationReviewViewModel FromDto(OfficerApplicationReviewDto dto, Guid? currentOfficerId) => new()
    {
        Dto = dto,
        IsAssignedToCurrentOfficer = dto.Application.AssignedOfficerId.HasValue
            && dto.Application.AssignedOfficerId == currentOfficerId
    };
}