namespace ATLAS.API.Requests;

/// <summary>
/// Request model for assigning an application to an officer
/// </summary>
public record AssignToOfficerRequest(
    Guid ApplicationId,
    Guid OfficerId
);
