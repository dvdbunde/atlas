namespace ATLAS.API.Requests;

/// <summary>
/// Request model for creating a new permit type
/// </summary>
public record CreatePermitTypeRequest(
    string Name,
    string? Description,
    decimal Fee,
    bool IsActive
);
