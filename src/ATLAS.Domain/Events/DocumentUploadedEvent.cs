using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class DocumentUploadedEvent : INotification
    {
        public Guid DocumentId { get; }
        public Guid ApplicationId { get; }
        public Guid UploadedById { get; }
        public string FileName { get; }
        public DateTime Timestamp { get; }

        public DocumentUploadedEvent(Guid documentId, Guid applicationId, Guid uploadedById, string fileName)
        {
            DocumentId = documentId;
            ApplicationId = applicationId;
            UploadedById = uploadedById;
            FileName = fileName;
            Timestamp = DateTime.UtcNow;
        }
    }
}
