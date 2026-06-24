using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    /// <summary>
    /// Raised when a document has been successfully accessed/downloaded.
    /// Represents successful document access for audit/tracking purposes.
    /// </summary>
    public class DocumentDownloadedEvent : INotification
    {
        public Guid DocumentId { get; }
        public Guid ApplicationId { get; }
        public Guid DownloadedById { get; }
        public string BlobUrl { get; }
        public DateTime Timestamp { get; }

        public DocumentDownloadedEvent(Guid documentId, Guid applicationId, Guid downloadedById, string blobUrl)
        {
            DocumentId = documentId;
            ApplicationId = applicationId;
            DownloadedById = downloadedById;
            BlobUrl = blobUrl;
            Timestamp = DateTime.UtcNow;
        }
    }
}