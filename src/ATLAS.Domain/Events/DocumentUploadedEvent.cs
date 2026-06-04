using System;

namespace ATLAS.Domain.Events
{
    public class DocumentUploadedEvent
    {
        public Guid DocumentId { get; }
        public Guid ApplicationId { get; }
        public Guid UserId { get; }
        public string FileName { get; }
        public DateTime Timestamp { get; }

        public DocumentUploadedEvent(Guid documentId, Guid applicationId, Guid userId, string fileName)
        {
            DocumentId = documentId;
            ApplicationId = applicationId;
            UserId = userId;
            FileName = fileName;
            Timestamp = DateTime.UtcNow;
        }
    }
}
