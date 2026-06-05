using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    public class ApplicationUnderReviewEvent : INotification
    {
        public Guid ApplicationId { get; }
        public Guid OfficerId { get; }
        public DateTime Timestamp { get; }

        public ApplicationUnderReviewEvent(Guid applicationId, Guid officerId)
        {
            ApplicationId = applicationId;
            OfficerId = officerId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
