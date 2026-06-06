using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class ApplicationSubmittedEvent : INotification
    {
        public Guid ApplicationId { get; }
        public Guid CitizenId { get; }
        public Guid PermitTypeId { get; }
        public DateTime Timestamp { get; }

        public ApplicationSubmittedEvent(Guid applicationId, Guid citizenId, Guid permitTypeId)
        {
            ApplicationId = applicationId;
            CitizenId = citizenId;
            PermitTypeId = permitTypeId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
