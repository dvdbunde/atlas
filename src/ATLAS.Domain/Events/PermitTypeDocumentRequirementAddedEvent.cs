using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class PermitTypeDocumentRequirementAddedEvent : INotification
    {
        public Guid PermitTypeId { get; }
        public Guid DocumentRequirementId { get; }
        public string DocumentType { get; }
        public DateTime Timestamp { get; }

        public PermitTypeDocumentRequirementAddedEvent(Guid permitTypeId, Guid documentRequirementId, string documentType)
        {
            PermitTypeId = permitTypeId;
            DocumentRequirementId = documentRequirementId;
            DocumentType = documentType;
            Timestamp = DateTime.UtcNow;
        }
    }
}
