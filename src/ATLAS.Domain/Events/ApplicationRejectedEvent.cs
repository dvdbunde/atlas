using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class ApplicationRejectedEvent : INotification
    {
        public Guid ApplicationId { get; }
        public Guid OfficerId { get; }
        public string ReasonCode { get; }
        public DateTime Timestamp { get; }

        public ApplicationRejectedEvent(Guid applicationId, Guid officerId, string reasonCode)
        {
            ApplicationId = applicationId;
            OfficerId = officerId;
            ReasonCode = reasonCode;
            Timestamp = DateTime.UtcNow;
        }
    }
}
