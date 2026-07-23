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
        public string BlobUrl { get; }
        public DateTime Timestamp { get; }

        public DocumentDownloadedEvent(Guid documentId, Guid applicationId, string blobUrl)
        {
            DocumentId = documentId;
            ApplicationId = applicationId;
            BlobUrl = blobUrl;
            Timestamp = DateTime.UtcNow;
        }
    }
}