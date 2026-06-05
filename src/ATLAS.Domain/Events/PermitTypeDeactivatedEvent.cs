using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class PermitTypeDeactivatedEvent : INotification
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
