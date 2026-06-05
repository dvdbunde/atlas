using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class ApplicationResubmittedEvent : INotification
    {
        public Guid ApplicationId { get; }
        public Guid CitizenId { get; }
        public DateTime Timestamp { get; }

        public ApplicationResubmittedEvent(Guid applicationId, Guid citizenId)
        {
            ApplicationId = applicationId;
            CitizenId = citizenId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
