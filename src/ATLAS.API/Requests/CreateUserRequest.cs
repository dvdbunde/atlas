namespace ATLAS.API.Requests;

/// <summary>
/// Request model for creating a new user
/// </summary>
public record CreateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string? Department,
    string Password
);
