using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class ApplicationResubmittedEvent : INotification
    {
        public Guid ApplicationId { get; }
        public DateTime Timestamp { get; }

        public ApplicationResubmittedEvent(Guid applicationId)
        {
            ApplicationId = applicationId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
