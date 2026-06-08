namespace ATLAS.API.Requests;

/// <summary>
/// Request model for uploading a document
/// </summary>
public record UploadDocumentRequest(
    Guid ApplicationId,
    string FileName,
    string ContentType,
    long FileSize,
    string BlobUrl,
    Guid UploadedById
);
