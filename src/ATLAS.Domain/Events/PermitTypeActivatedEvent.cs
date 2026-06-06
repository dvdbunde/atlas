using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class PermitTypeActivatedEvent : INotification
    {
        public Guid PermitTypeId { get; }
        public Guid? ActivatedByAdminId { get; }
        public DateTime Timestamp { get; }

        public PermitTypeActivatedEvent(Guid permitTypeId, Guid? activatedByAdminId = null)
        {
            PermitTypeId = permitTypeId;
            ActivatedByAdminId = activatedByAdminId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
