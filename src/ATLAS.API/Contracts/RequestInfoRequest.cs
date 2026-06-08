namespace ATLAS.API.Requests;

/// <summary>
/// Request model for requesting additional information for an application
/// </summary>
public record RequestInfoRequest(
    Guid ApplicationId,
    Guid OfficerId,
    string Message    
);
