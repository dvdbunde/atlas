namespace ATLAS.API.Requests;

/// <summary>
/// Request model for updating an existing permit type
/// </summary>
public record UpdatePermitTypeRequest(
    Guid PermitTypeId,
    string Name,
    string? Description,    
    int? EstimatedProcessingDays,
    Guid DeactivatedByAdminId,
    bool IsActive
);
