using System;

namespace ATLAS.Domain.Events
{
    public class PermitTypeActivatedEvent
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
