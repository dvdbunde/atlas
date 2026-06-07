namespace ATLAS.API.Requests;

/// <summary>
/// Request model for rejecting a permit application
/// </summary>
public record RejectApplicationRequest(
    Guid ApplicationId,
    string ReasonCode,
    string? Comments
);
