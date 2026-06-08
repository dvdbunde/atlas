namespace ATLAS.API.Requests;

/// <summary>
/// Request model for submitting a new permit application
/// </summary>
public record SubmitApplicationRequest(
    Guid CitizenId,
    Guid PermitTypeId,
    string? CitizenNotes
);
