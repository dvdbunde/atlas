using System;

namespace ATLAS.Domain.Events
{
    /// <summary>
    /// Domain event raised when a citizen resubmits an application after info was requested.
    /// </summary>
    public class ApplicationResubmittedEvent
    {
        public Guid ApplicationId { get; }
        public Guid CitizenId { get; }

        public ApplicationResubmittedEvent(Guid applicationId, Guid citizenId)
        {
            ApplicationId = applicationId;
            CitizenId = citizenId;
        }
    }
}