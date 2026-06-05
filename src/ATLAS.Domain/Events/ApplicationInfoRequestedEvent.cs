using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class ApplicationInfoRequestedEvent : INotification
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
