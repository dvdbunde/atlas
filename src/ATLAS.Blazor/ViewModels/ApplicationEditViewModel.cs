using ATLAS.Application.DTOs;
using ATLAS.Domain.Enums;

namespace ATLAS.Blazor.ViewModels;

/// <summary>
/// View model for the Application Edit page.
/// Manages page state during the draft editing flow.
/// </summary>
public class ApplicationEditViewModel
{
    public Guid ApplicationId { get; set; }
    public Guid PermitTypeId { get; set; }
    public string PermitName { get; set; } = string.Empty;
    public string PermitDescription { get; set; } = string.Empty;
    public string ApplicationNumber { get; set; } = string.Empty;
    public ApplicationStatus Status { get; set; }
    public List<DynamicFormFieldViewModel> Fields { get; set; } = new();
    public bool IsLoading { get; set; } = true;
    public bool IsSaving { get; set; }
    public bool HasError { get; set; }
    public string? ErrorMessage { get; set; }
    public bool SaveSuccess { get; set; }
    public bool CreatedSuccess { get; set; } = false;
    public bool IsLoaded => !IsLoading && !HasError;
    public bool IsSubmitting { get; set; }
    public bool SubmitHasError { get; set; }
    public string? SubmitErrorMessage { get; set; }

    // O5: Information request display
    public string? InfoRequestMessage { get; set; }
    public string? InfoRequestDateDisplay { get; set; }
    public string? InfoRequestOfficerName { get; set; }
    public bool IsInfoRequested => Status == ApplicationStatus.InfoRequested;

    // O5: Resubmit state
    public bool IsResubmitting { get; set; }
    public bool ResubmitHasError { get; set; }
    public string? ResubmitErrorMessage { get; set; }
    public bool ResubmitSuccess { get; set; }

    /// <summary>
    /// Loads the page from application and permit type data.
    /// Merges field definitions with existing values.
    /// </summary>
    public void Load(
        ApplicationDetailDto application,
        PermitTypeDto permitType)
    {
        ApplicationId = application.Id;
        PermitTypeId = permitType.Id;
        PermitName = permitType.Name;
        PermitDescription = permitType.Description;
        ApplicationNumber = application.ApplicationNumber;
        Status = application.Status;

        // O5: Extract latest information request from review history
        var infoRequest = application.Reviews?
            .FirstOrDefault(r => r.Decision == ReviewDecision.RequestInfo);
        InfoRequestMessage = infoRequest?.Comments;
        InfoRequestDateDisplay = infoRequest?.ReviewedDate.ToString("MMM dd, yyyy");
        InfoRequestOfficerName = application.OfficerName;
    
        var fields = permitType.Fields.Select(fd =>
        {
            var hasExistingValue = application.FieldValues.TryGetValue(fd.Name, out var existingValue);
    
            return new DynamicFormFieldViewModel
            {
                FieldName = fd.Name,
                Label = fd.Name,
                Type = fd.Type,
                IsRequired = fd.IsRequired,
                DefaultValue = fd.DefaultValue,
                CurrentValue = hasExistingValue ? existingValue : (fd.DefaultValue ?? string.Empty),
                Options = fd.Options ?? new(),
                SortOrder = 0,
                AllowedExtensions = fd.AllowedExtensions,
                MaxFileSizeBytes = fd.MaxFileSizeBytes
            };
        }).ToList();
    
        // D4: Attach uploaded documents to FileUpload fields (field name matches document type)
        foreach (var field in fields.Where(f => f.Type == FieldType.FileUpload))
        {
            var matchingDocs = application.Documents
                .Where(d => d.DocumentType.StartsWith(field.FieldName, StringComparison.OrdinalIgnoreCase))
                .ToList();
            field.UploadedDocuments = matchingDocs;
        }
    
        Fields = fields;
    }
}