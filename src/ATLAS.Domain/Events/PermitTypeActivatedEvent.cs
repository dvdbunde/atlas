using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class PermitTypeActivatedEvent : INotification
    {
        public Guid PermitTypeId { get; }
        public DateTime Timestamp { get; }

        public PermitTypeActivatedEvent(Guid permitTypeId)
        {
            PermitTypeId = permitTypeId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
