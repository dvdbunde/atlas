using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class ApplicationUnderReviewEvent : INotification
    {
        public Guid ApplicationId { get; }
        public DateTime Timestamp { get; }

        public ApplicationUnderReviewEvent(Guid applicationId)
        {
            ApplicationId = applicationId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
