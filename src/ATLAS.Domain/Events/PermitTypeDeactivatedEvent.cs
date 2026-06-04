using System;

namespace ATLAS.Domain.Events
{
    public class PermitTypeDeactivatedEvent
    {
        public Guid PermitTypeId { get; }
        public Guid DeactivatedByAdminId { get; }
        public DateTime Timestamp { get; }

        public PermitTypeDeactivatedEvent(Guid permitTypeId, Guid deactivatedByAdminId)
        {
            PermitTypeId = permitTypeId;
            DeactivatedByAdminId = deactivatedByAdminId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
