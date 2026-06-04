using System;

namespace ATLAS.Domain.Events
{
    public class ApplicationApprovedEvent
    {
        public Guid ApplicationId { get; }
        public Guid OfficerId { get; }
        public DateTime Timestamp { get; }

        public ApplicationApprovedEvent(Guid applicationId, Guid officerId)
        {
            ApplicationId = applicationId;
            OfficerId = officerId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
