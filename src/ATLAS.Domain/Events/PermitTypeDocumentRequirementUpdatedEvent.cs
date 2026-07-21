using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class PermitTypeDocumentRequirementUpdatedEvent : INotification
    {
        public Guid PermitTypeId { get; }
        public Guid DocumentRequirementId { get; }
        public string DocumentType { get; }
        public bool IsRequired { get; }
        public DateTime Timestamp { get; }

        public PermitTypeDocumentRequirementUpdatedEvent(Guid permitTypeId, Guid documentRequirementId, string documentType, bool isRequired)
        {
            PermitTypeId = permitTypeId;
            DocumentRequirementId = documentRequirementId;
            DocumentType = documentType;
            IsRequired = isRequired;
            Timestamp = DateTime.UtcNow;
        }
    }
}
