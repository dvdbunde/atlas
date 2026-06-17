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
    public bool IsLoaded => !IsLoading && !HasError;

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

        Fields = permitType.Fields.Select(fd =>
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
                SortOrder = 0
            };
        }).ToList();
    }
}