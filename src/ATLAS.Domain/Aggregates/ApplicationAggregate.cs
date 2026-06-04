using System.Collections.Generic;
using System.Linq;
using ATLAS.Domain.Entities;
using ATLAS.Domain.ValueObjects;

namespace ATLAS.Domain.Aggregates
{
    // Application Aggregate Root - Ensures application state transitions are valid
    // Contains Document and Review entities
    public class ApplicationAggregate
    {
        private readonly Application _application;
        private readonly List<Document> _documents;
        private readonly List<Review> _reviews;

        public Application Application => _application;
        public IReadOnlyList<Document> Documents => _documents.AsReadOnly();
        public IReadOnlyList<Review> Reviews => _reviews.AsReadOnly();

        public ApplicationAggregate(Application application)
        {
            _application = application ?? throw new ArgumentNullException(nameof(application));
            _documents = new List<Document>();
            _reviews = new List<Review>();
        }

        // Only allow adding documents through the aggregate root
        public void AddDocument(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            
            if (_application.Status == ApplicationStatus.Approved || 
                _application.Status == ApplicationStatus.Rejected)
                throw new DomainException("Cannot add documents to approved or rejected applications");

            _documents.Add(document);
        }

        // Only allow adding reviews through the aggregate root
        public void AddReview(Review review)
        {
            if (review == null)
                throw new ArgumentNullException(nameof(review));
            
            if (_application.Status != ApplicationStatus.UnderReview)
                throw new DomainException("Can only add reviews for applications under review");

            _reviews.Add(review);
        }

        // Enforce invariants
        public void ValidateInvariants()
        {
            // Invariant 1: Status transitions must be valid
            // (enforced by Application entity methods)

            // Invariant 2: Rejection requires a reason code
            var latestReview = _reviews.OrderByDescending(r => r.ReviewedDate).FirstOrDefault();
            if (latestReview != null && latestReview.Decision == ReviewDecision.Reject)
            {
                if (string.IsNullOrWhiteSpace(latestReview.ReasonCode))
                    throw new DomainException("Rejection requires a reason code");
            }

            // Invariant 3: Documents must belong to the application
            foreach (var doc in _documents)
            {
                if (doc.ApplicationId != _application.Id)
                    throw new DomainException("Document does not belong to this application");
            }

            // Invariant 4: Only one active review at a time
            // (enforced by business logic - new review creates history)

            // Invariant 5: Application must have a valid PermitType
            // (checked during submission)

            // Invariant 6: Submitted date set on first submission
            // (enforced by Application.Submit())
        }
    }
}