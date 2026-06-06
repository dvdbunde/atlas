using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class ApplicationAssignedToOfficerEvent : INotification
    {
        public Guid ApplicationId { get; }
        public Guid OfficerId { get; }
        public DateTime Timestamp { get; }

        public ApplicationAssignedToOfficerEvent(Guid applicationId, Guid officerId)
        {
            ApplicationId = applicationId;
            OfficerId = officerId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
