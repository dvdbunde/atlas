namespace ATLAS.API.Requests;

/// <summary>
/// Request model for approving a permit application
/// </summary>
public record ApproveApplicationRequest(
    Guid ApplicationId,    
    Guid OfficerId,
    string Comments
);
