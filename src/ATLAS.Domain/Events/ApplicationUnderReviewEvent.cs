using System;

namespace ATLAS.Domain.Events
{
    /// <summary>
    /// Domain event raised when an application review starts.
    /// </summary>
    public class ApplicationUnderReviewEvent
    {
        public Guid ApplicationId { get; }
        public Guid OfficerId { get; }

        public ApplicationUnderReviewEvent(Guid applicationId, Guid officerId)
        {
            ApplicationId = applicationId;
            OfficerId = officerId;
        }
    }
}