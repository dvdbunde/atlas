using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class ApplicationInfoRequestedEvent : INotification
    {
        public Guid ApplicationId { get; }
        public string Message { get; }
        public DateTime Timestamp { get; }

        public ApplicationInfoRequestedEvent(Guid applicationId, string message)
        {
            ApplicationId = applicationId;
            Message = message;
            Timestamp = DateTime.UtcNow;
        }
    }
}
