using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class PermitTypeDeactivatedEvent : INotification
    {
        public Guid PermitTypeId { get; }
        public DateTime Timestamp { get; }

        public PermitTypeDeactivatedEvent(Guid permitTypeId)
        {
            PermitTypeId = permitTypeId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
