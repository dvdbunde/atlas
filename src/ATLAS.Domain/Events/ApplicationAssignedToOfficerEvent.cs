using System;

namespace ATLAS.Domain.Events
{
    public class ApplicationAssignedToOfficerEvent
    {
        public Guid ApplicationId { get; }
        public Guid OfficerId { get; }
        public DateTime OccurredOn { get; }

        public ApplicationAssignedToOfficerEvent(Guid applicationId, Guid officerId)
        {
            ApplicationId = applicationId;
            OfficerId = officerId;
            OccurredOn = DateTime.UtcNow;
        }
    }
}
