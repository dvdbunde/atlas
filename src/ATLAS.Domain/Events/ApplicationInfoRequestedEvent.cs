using System;

namespace ATLAS.Domain.Events
{
    public class ApplicationInfoRequestedEvent
    {
        public Guid ApplicationId { get; }
        public Guid OfficerId { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }

        public ApplicationInfoRequestedEvent(Guid applicationId, Guid officerId, string message)
        {
            ApplicationId = applicationId;
            OfficerId = officerId;
            Message = message;
            Timestamp = DateTime.UtcNow;
        }
    }
}
