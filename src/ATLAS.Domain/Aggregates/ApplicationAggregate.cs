using System.Collections.Generic;
using System.Linq;
using ATLAS.Domain.Entities;
using ATLAS.Domain.ValueObjects;

namespace ATLAS.Domain.Aggregates
{
    /// <summary>
    /// Application Aggregate Root - Enforces invariants for Application and its child entities.
    /// Uses Application.Documents and Application.Reviews directly (no duplicate state).
    /// </summary>
    public class ApplicationAggregate
    {
        private readonly Application _application;

        public Application Application => _application;
        public IReadOnlyList<Document> Documents => _application.Documents;
        public IReadOnlyList<Review> Reviews => _application.Reviews;

        public ApplicationAggregate(Application application)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
        }

        /// <summary>
        /// Enforces all aggregate invariants.
        /// </summary>
        /// <exception cref="DomainException">Thrown when invariants are violated</exception>
        public void ValidateInvariants()
        {
            // Invariant 1: Status transitions must be valid
            // (enforced by Application entity methods - Submit, Approve, Reject, etc.)

            // Invariant 2: Rejection requires a reason code
            var latestReview = _application.Reviews
                .OrderByDescending(r => r.ReviewedDate)
                .FirstOrDefault();
            
            if (latestReview != null && latestReview.Decision == ReviewDecision.Reject)
            {
                if (string.IsNullOrWhiteSpace(latestReview.ReasonCode))
                    throw new DomainException("Rejection requires a reason code");
            }

            // Invariant 3: Documents must belong to the application
            foreach (var doc in _application.Documents)
            {
                if (doc.ApplicationId != _application.Id)
                    throw new DomainException("Document does not belong to this application");
            }

            // Invariant 4: Only one active review at a time
            var hasFinalReview = _application.Reviews
                .Any(r => r.Decision == ReviewDecision.Approve || r.Decision == ReviewDecision.Reject);
            
            if (hasFinalReview && _application.Status == ApplicationStatus.UnderReview)
            {
                throw new DomainException("Application already has a final review but is still under review");
            }

            // Invariant 5: Application must have a valid PermitType
            // (checked in Application.Submit() - PermitTypeId must not be empty)

            // Invariant 6: Submitted date set on first submission
            // (enforced by Application.Submit())
        }
    }
}