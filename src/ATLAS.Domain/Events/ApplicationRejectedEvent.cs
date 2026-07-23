using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class ApplicationRejectedEvent : INotification
    {
        public Guid ApplicationId { get; }
        public string ReasonCode { get; }
        public DateTime Timestamp { get; }

        public ApplicationRejectedEvent(Guid applicationId, string reasonCode)
        {
            ApplicationId = applicationId;
            ReasonCode = reasonCode;
            Timestamp = DateTime.UtcNow;
        }
    }
}
