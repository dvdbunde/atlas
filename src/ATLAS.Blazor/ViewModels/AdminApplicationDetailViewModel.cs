using ATLAS.Application.DTOs;
using ATLAS.Domain.Enums;

namespace ATLAS.Blazor.ViewModels;

/// <summary>
/// View model for the Administrator Application Details page.
/// Provides a complete read-only projection of an application using
/// existing DTOs and shared view model types.
/// </summary>
public class AdminApplicationDetailViewModel
{
    // Application identity
    public Guid ApplicationId { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;

    // General information
    public string PermitTypeName { get; set; } = string.Empty;
    public string PermitTypeDescription { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? LastUpdated { get; set; }

    // Applicant information
    public string CitizenName { get; set; } = string.Empty;
    public string CitizenEmail { get; set; } = string.Empty;

    // Assignment
    public string? AssignedOfficerName { get; set; }
    public bool IsAssigned => !string.IsNullOrEmpty(AssignedOfficerName);

    // Application data
    public List<FieldDisplayViewModel> Fields { get; set; } = new();

    // Documents (flat list, grouped by requirement type in the UI)
    public List<DocumentDto> Documents { get; set; } = new();

    // Reviews / comments
    public List<ReviewDisplayViewModel> Reviews { get; set; } = new();
    public bool HasReviews => Reviews.Count > 0;

    // Officer notes (full text)
    public string OfficerNotes { get; set; } = string.Empty;
    public bool HasOfficerNotes => !string.IsNullOrWhiteSpace(OfficerNotes);

    // Citizen notes
    public string CitizenNotes { get; set; } = string.Empty;

    // Activity / timeline
    public List<ApplicationActivityDto> Activities { get; set; } = new();

    // Loading / error state
    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsLoaded => !IsLoading && !HasError;

    // Display helpers
    public string SubmittedDateDisplay => SubmittedDate?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "Not submitted";
    public string LastUpdatedDisplay => LastUpdated?.ToLocalTime().ToString("dd/MM/yyyy HH:mm") ?? "N/A";
    public string AssignedOfficerDisplay => AssignedOfficerName ?? "Unassigned";

    public void Load(ApplicationDetailDto application, PermitTypeDto permitType)
    {
        ApplicationId = application.Id;
        ApplicationNumber = application.ApplicationNumber;
        PermitTypeName = permitType.Name;
        PermitTypeDescription = permitType.Description;
        Status = application.Status;
        SubmittedDate = application.SubmittedDate;
        LastUpdated = application.ReviewedDate ?? application.SubmittedDate;
        CitizenName = application.CitizenName ?? "Unknown";
        OfficerNotes = application.OfficerNotes;
        CitizenNotes = application.CitizenNotes;
        AssignedOfficerName = application.AssignedOfficerName;
        Documents = application.Documents.ToList();

        // Map field definitions with existing values
        var fieldList = new List<FieldDisplayViewModel>();
        foreach (var fd in permitType.Fields)
        {
            if (fd.Type == FieldType.FileUpload)
            {
                var matchingDocs = application.Documents
                    .Where(d => d.DocumentType.StartsWith(fd.Name, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                fieldList.Add(new FieldDisplayViewModel
                {
                    Label = fd.Name,
                    Value = string.Empty,
                    Type = FieldType.FileUpload,
                    Documents = matchingDocs
                });
            }
            else
            {
                application.FieldValues.TryGetValue(fd.Name, out var existingValue);
                var val = existingValue ?? fd.DefaultValue ?? string.Empty;
                if (!string.IsNullOrEmpty(val))
                {
                    fieldList.Add(new FieldDisplayViewModel
                    {
                        Label = fd.Name,
                        Value = val
                    });
                }
            }
        }
        Fields = fieldList;

        // All reviews visible to admin (not filtered by IsVisibleToCitizen)
        Reviews = application.Reviews
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

    public void LoadCitizenEmail(string email)
    {
        CitizenEmail = email ?? string.Empty;
    }
}

