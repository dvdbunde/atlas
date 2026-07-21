using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class PermitTypeDocumentRequirementsReorderedEvent : INotification
    {
        public Guid PermitTypeId { get; }
        public DateTime Timestamp { get; }

        public PermitTypeDocumentRequirementsReorderedEvent(Guid permitTypeId)
        {
            PermitTypeId = permitTypeId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
