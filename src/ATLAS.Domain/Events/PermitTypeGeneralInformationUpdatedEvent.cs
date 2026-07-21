using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class PermitTypeGeneralInformationUpdatedEvent : INotification
    {
        public Guid PermitTypeId { get; }
        public string Name { get; }
        public string Description { get; }
        public DateTime Timestamp { get; }

        public PermitTypeGeneralInformationUpdatedEvent(Guid permitTypeId, string name, string description)
        {
            PermitTypeId = permitTypeId;
            Name = name;
            Description = description;
            Timestamp = DateTime.UtcNow;
        }
    }
}
