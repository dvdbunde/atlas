using ATLAS.Application.DTOs;
using ATLAS.Domain.Enums;

namespace ATLAS.Blazor.ViewModels;

/// <summary>
/// View model for the Confirmation page shown after successful submission.
/// </summary>
public class ConfirmationViewModel
{
    public Guid ApplicationId { get; set; }
    public string ApplicationNumber { get; set; } = string.Empty;
    public string PermitTypeName { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public string SubmittedDateDisplay => SubmittedDate?.ToString("MMM dd, yyyy h:mm tt") ?? "N/A";

    public bool IsLoading { get; set; } = true;
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsLoaded => !IsLoading && !HasError;

    public void Load(ApplicationDetailDto application, PermitTypeDto permitType)
    {
        ApplicationId = application.Id;
        ApplicationNumber = application.ApplicationNumber;
        PermitTypeName = permitType.Name;
        Status = application.Status;
        SubmittedDate = application.SubmittedDate;
    }
}