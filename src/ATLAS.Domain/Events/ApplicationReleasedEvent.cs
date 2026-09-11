using System;
using MediatR;

namespace ATLAS.Domain.Events
{
    /// <summary>
    /// Raised when the currently assigned officer releases their own assignment,
    /// returning the application to the unassigned Submitted pool.
    /// </summary>
    public class ApplicationReleasedEvent : INotification
    {
        public Guid ApplicationId { get; }
        public DateTime Timestamp { get; }

        public ApplicationReleasedEvent(Guid applicationId)
        {
            ApplicationId = applicationId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
