using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class ApplicationAssignedToOfficerEvent : INotification
    {
        public Guid ApplicationId { get; }
        public DateTime Timestamp { get; }

        public ApplicationAssignedToOfficerEvent(Guid applicationId)
        {
            ApplicationId = applicationId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
