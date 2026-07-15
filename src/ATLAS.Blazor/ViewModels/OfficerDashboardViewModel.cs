using ATLAS.Application.DTOs;
using ATLAS.Domain.Enums;

namespace ATLAS.Blazor.ViewModels;

/// <summary>Sort options exposed to the UI (mirrors OfficerDashboardSortBy).</summary>
public enum OfficerDashboardSortOption
{
    LastUpdated,
    SubmittedDate
}

public class OfficerDashboardViewModel
{
    public List<OfficerApplicationCardViewModel> Applications { get; set; } = new();
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsEmpty => !IsLoading && !HasError && Applications.Count == 0;

    // Filters
    public List<ApplicationStatus> SelectedStatuses { get; set; } = new();
    public Guid? SelectedPermitTypeId { get; set; }

    // Sorting
    public OfficerDashboardSortOption SortBy { get; set; } = OfficerDashboardSortOption.LastUpdated;
    public bool SortDescending { get; set; } = true;

    // Pagination
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 12;
    public int TotalCount { get; set; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    // Filter options
    public List<PermitTypeFilterOption> PermitTypes { get; set; } = new();
}

public class PermitTypeFilterOption
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>Card-level view model for a single application on the officer dashboard.</summary>
public class OfficerApplicationCardViewModel
{
    public Guid ApplicationId { get; init; }
    public string ApplicationNumber { get; init; } = string.Empty;
    public string PermitTypeName { get; init; } = string.Empty;
    public ApplicationStatus Status { get; init; }
    public string CitizenName { get; init; } = string.Empty;
    public DateTime? SubmittedDate { get; init; }
    public DateTime? LastUpdated { get; init; }
    public string? AssignedOfficerName { get; init; }
    public int DocumentCount { get; init; }
    public bool AllRequiredDocumentsUploaded { get; init; }

    public string SubmittedDateDisplay => SubmittedDate?.ToString("MMM dd, yyyy") ?? "Not submitted";
    public string LastUpdatedDisplay => LastUpdated?.ToString("MMM dd, yyyy") ?? "N/A";
    public string AssignedOfficerDisplay => AssignedOfficerName ?? "Unassigned";
    public string NavigationUrl => $"/officer/applications/{ApplicationId}";
    public string ActionLabel => "Open Application";

    public static OfficerApplicationCardViewModel FromDto(OfficerDashboardDto dto) => new()
    {
        ApplicationId = dto.ApplicationId,
        ApplicationNumber = dto.ApplicationNumber,
        PermitTypeName = dto.PermitTypeName,
        Status = dto.Status,
        CitizenName = dto.CitizenName,
        SubmittedDate = dto.SubmittedDate,
        LastUpdated = dto.LastUpdated,
        AssignedOfficerName = dto.AssignedOfficerName,
        DocumentCount = dto.DocumentCount,
        AllRequiredDocumentsUploaded = dto.AllRequiredDocumentsUploaded
    };
}