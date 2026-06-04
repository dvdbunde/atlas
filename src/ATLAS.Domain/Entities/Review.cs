using System;

namespace ATLAS.Domain.Entities
{
    public class Review : Entity<Guid>
    {
        public Guid ApplicationId { get; private set; }
        public Guid OfficerId { get; private set; }
        public ReviewDecision Decision { get; private set; }
        public string ReasonCode { get; private set; }
        public string Comments { get; private set; }
        public DateTime ReviewedDate { get; private set; }
        public bool IsVisibleToCitizen { get; private set; }

        public Review(Guid id, Guid applicationId, Guid officerId, ReviewDecision decision, string comments, bool isVisibleToCitizen)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("Review ID cannot be empty", nameof(id));
            
            if (applicationId == Guid.Empty)
                throw new ArgumentException("Application ID cannot be empty", nameof(applicationId));
            
            if (officerId == Guid.Empty)
                throw new ArgumentException("Officer ID cannot be empty", nameof(officerId));

            Id = id;
            ApplicationId = applicationId;
            OfficerId = officerId;
            Decision = decision;
            ReasonCode = decision == ReviewDecision.Reject ? "IncompleteApplication" : null;
            Comments = comments ?? string.Empty;
            ReviewedDate = DateTime.UtcNow;
            IsVisibleToCitizen = isVisibleToCitizen;
        }

        protected Review()
        {
        }
    }
}
