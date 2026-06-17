using ATLAS.Application.DTOs;
using ATLAS.Domain.Enums;

namespace ATLAS.Blazor.ViewModels;

/// <summary>
/// View model for the Application Detail page.
/// Contains the application detail, related permit type metadata, timeline, and reviews.
/// Read-only — no editing possible.
/// </summary>
public class ApplicationDetailViewModel
{
    public Guid ApplicationId { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public string PermitTypeName { get; set; } = string.Empty;
    public string PermitTypeDescription { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public string CitizenNotes { get; set; } = string.Empty;
    public string OfficerNotes { get; set; } = string.Empty;
    public string? OfficerName { get; set; }

    public List<FieldDisplayViewModel> Fields { get; set; } = new();
    public List<TimelineEntryViewModel> TimelineEntries { get; set; } = new();
    public List<ReviewDisplayViewModel> Reviews { get; set; } = new();

    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsLoaded => !IsLoading && !HasError;
    public bool HasReviews => Reviews.Count > 0;
    public string SubmittedDateDisplay => SubmittedDate?.ToString("MMM dd, yyyy") ?? "N/A";
    public string ReviewedDateDisplay => ReviewedDate?.ToString("MMM dd, yyyy") ?? "N/A";

    public void Load(ApplicationDetailDto application, PermitTypeDto permitType)
    {
        ApplicationId = application.Id;
        ApplicationNumber = application.ApplicationNumber;
        PermitTypeName = permitType.Name;
        PermitTypeDescription = permitType.Description;
        Status = application.Status;
        SubmittedDate = application.SubmittedDate;
        ReviewedDate = application.ReviewedDate;
        CitizenNotes = application.CitizenNotes;
        OfficerNotes = application.OfficerNotes;
        OfficerName = application.OfficerName;

        // Map field definitions with existing values
        Fields = permitType.Fields.Select(fd =>
        {
            application.FieldValues.TryGetValue(fd.Name, out var existingValue);

            return new FieldDisplayViewModel
            {
                Label = fd.Name,
                Value = existingValue ?? fd.DefaultValue ?? string.Empty
            };
        }).Where(f => !string.IsNullOrEmpty(f.Value))
          .ToList();

        // Build timeline entries
        TimelineEntries = BuildTimeline(application.Status);

        // Filter reviews visible to citizen
        Reviews = application.Reviews
            .Where(r => r.IsVisibleToCitizen)
            .Select(r => new ReviewDisplayViewModel
            {
                Decision = r.Decision,
                Comments = r.Comments ?? string.Empty,
                ReviewedDate = r.ReviewedDate,
                ReasonCode = r.ReasonCode
            })
            .OrderByDescending(r => r.ReviewedDate)
            .ToList();
    }

    private static List<TimelineEntryViewModel> BuildTimeline(ApplicationStatus currentStatus)
    {
        var allStatuses = new[]
        {
            (ApplicationStatus.Draft, "Draft"),
            (ApplicationStatus.Submitted, "Submitted"),
            (ApplicationStatus.UnderReview, "Under Review"),
            (ApplicationStatus.InfoRequested, "Info Requested"),
            (ApplicationStatus.Resubmitted, "Resubmitted"),
            (ApplicationStatus.Approved, "Approved"),
            (ApplicationStatus.Rejected, "Rejected")
        };

        var entries = new List<TimelineEntryViewModel>();
        var currentIndex = Array.IndexOf(allStatuses, allStatuses.First(s => s.Item1 == currentStatus));

        for (int i = 0; i < allStatuses.Length; i++)
        {
            var (status, label) = allStatuses[i];

            TimelineEntryState state;
            if (i < currentIndex)
                state = TimelineEntryState.Completed;
            else if (i == currentIndex)
                state = TimelineEntryState.Current;
            else
                state = TimelineEntryState.Future;

            // Rejected is a terminal state — mark subsequent as skipped
            if (currentStatus == ApplicationStatus.Rejected && i > currentIndex)
                state = TimelineEntryState.Skipped;

            entries.Add(new TimelineEntryViewModel
            {
                Status = status,
                Label = label,
                State = state
            });
        }

        return entries;
    }
}

/// <summary>
/// Read-only field display for the detail page.
/// </summary>
public class FieldDisplayViewModel
{
    public string Label { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
}

/// <summary>
/// A single entry in the application timeline.
/// </summary>
public class TimelineEntryViewModel
{
    public ApplicationStatus Status { get; init; }
    public string Label { get; init; } = string.Empty;
    public TimelineEntryState State { get; init; }
}

public enum TimelineEntryState
{
    Completed,
    Current,
    Future,
    Skipped
}

/// <summary>
/// A review displayed to the citizen.
/// </summary>
public class ReviewDisplayViewModel
{
    public ReviewDecision Decision { get; init; }
    public string Comments { get; init; } = string.Empty;
    public DateTime ReviewedDate { get; init; }
    public string? ReasonCode { get; init; }
    public string ReviewedDateDisplay => ReviewedDate.ToString("MMM dd, yyyy");
}