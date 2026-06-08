namespace ATLAS.API.Requests;

/// <summary>
/// Request model for updating a user's role
/// </summary>
public record UpdateUserRoleRequest(
    Guid UserId,
    string Role,
    string? Department
);
