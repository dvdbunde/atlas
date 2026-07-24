using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class ApplicationApprovedEvent : INotification
    {
        public Guid ApplicationId { get; }
        public DateTime Timestamp { get; }

        public ApplicationApprovedEvent(Guid applicationId)
        {
            ApplicationId = applicationId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
