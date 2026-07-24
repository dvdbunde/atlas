using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class PermitTypeFieldsReorderedEvent : INotification
    {
        public Guid PermitTypeId { get; }
        public DateTime Timestamp { get; }

        public PermitTypeFieldsReorderedEvent(Guid permitTypeId)
        {
            PermitTypeId = permitTypeId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
