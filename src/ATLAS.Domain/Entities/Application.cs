using System;
using System.Collections.Generic;
using ATLAS.Domain.ValueObjects;
using ATLAS.Domain.Events;

namespace ATLAS.Domain.Entities
{
    public class Application : Entity<Guid>
    {
        public string ApplicationNumber { get; private set; }
        public ApplicationStatus Status { get; private set; }
        public DateTime? SubmittedDate { get; private set; }
        public DateTime? ReviewedDate { get; private set; }
        public string CitizenNotes { get; private set; }
        public string OfficerNotes { get; private set; }
        public Guid CitizenId { get; private set; }
        public Guid PermitTypeId { get; private set; }
        
        private readonly List<Document> _documents = new();
        public IReadOnlyList<Document> Documents => _documents.AsReadOnly();
        
        private readonly List<Review> _reviews = new();
        public IReadOnlyList<Review> Reviews => _reviews.AsReadOnly();

        public Application(Guid citizenId, Guid permitTypeId, string citizenNotes)
        {
            if (citizenId == Guid.Empty)
                throw new ArgumentException("Citizen ID cannot be empty", nameof(citizenId));
            
            if (permitTypeId == Guid.Empty)
                throw new ArgumentException("Permit type ID cannot be empty", nameof(permitTypeId));

            Id = Guid.NewGuid();
            ApplicationNumber = GenerateApplicationNumber();
            Status = ApplicationStatus.Draft;
            CitizenId = citizenId;
            PermitTypeId = permitTypeId;
            CitizenNotes = citizenNotes ?? string.Empty;
            OfficerNotes = string.Empty;
        }

        protected Application()
        {
        }

        public void Submit()
        {
            if (Status != ApplicationStatus.Draft)
                throw new DomainException("Only draft applications can be submitted");

            Status = ApplicationStatus.Submitted;
            SubmittedDate = DateTime.UtcNow;
            AddDomainEvent(new ApplicationSubmittedEvent(Id, CitizenId, PermitTypeId));
        }

        public void StartReview(Guid officerId)
        {
            if (Status != ApplicationStatus.Submitted)
                throw new DomainException("Can only start review for submitted applications");

            Status = ApplicationStatus.UnderReview;
        }

        public void Approve(Guid officerId, string comments)
        {
            if (Status != ApplicationStatus.UnderReview)
                throw new DomainException("Only applications under review can be approved");

            Status = ApplicationStatus.Approved;
            ReviewedDate = DateTime.UtcNow;
            OfficerNotes += $"[APPROVED {DateTime.UtcNow} by {officerId}]: {comments}";
            AddDomainEvent(new ApplicationApprovedEvent(Id, officerId));
        }

        public void Reject(Guid officerId, string reasonCode, string comments)
        {
            if (Status != ApplicationStatus.UnderReview)
                throw new DomainException("Only applications under review can be rejected");

            if (string.IsNullOrWhiteSpace(reasonCode))
                throw new DomainException("Rejection requires a reason code");

            Status = ApplicationStatus.Rejected;
            ReviewedDate = DateTime.UtcNow;
            OfficerNotes += $"[REJECTED {DateTime.UtcNow} by {officerId}]: Reason: {reasonCode}. {comments}";
            AddDomainEvent(new ApplicationRejectedEvent(Id, officerId, reasonCode));
        }

        public void RequestInfo(Guid officerId, string message)
        {
            if (Status != ApplicationStatus.UnderReview)
                throw new DomainException("Can only request info for applications under review");

            Status = ApplicationStatus.InfoRequested;
            OfficerNotes += $"[INFO REQUESTED {DateTime.UtcNow} by {officerId}]: {message}";
            AddDomainEvent(new ApplicationInfoRequestedEvent(Id, officerId, message));
        }

        public void Resubmit(string updatedNotes)
        {
            if (Status != ApplicationStatus.InfoRequested)
                throw new DomainException("Only applications with info requested can be resubmitted");

            Status = ApplicationStatus.Resubmitted;
            CitizenNotes += $"[RESUBMITTED {DateTime.UtcNow}]: {updatedNotes}";
        }

        public void AddDocument(Guid documentId, string fileName, string contentType, long fileSize, string blobUrl, Guid uploadedById)
        {
            if (Status == ApplicationStatus.Approved || Status == ApplicationStatus.Rejected)
                throw new DomainException("Cannot add documents to approved or rejected applications");

            var document = new Document(documentId, Id, fileName, contentType, fileSize, blobUrl, uploadedById);
            _documents.Add(document);
            AddDomainEvent(new DocumentUploadedEvent(documentId, Id, uploadedById, fileName));
        }

        public void AddReview(Guid reviewId, Guid officerId, ReviewDecision decision, string comments, bool isVisibleToCitizen)
        {
            if (Status != ApplicationStatus.UnderReview)
                throw new DomainException("Can only add reviews for applications under review");

            var review = new Review(reviewId, Id, officerId, decision, comments, isVisibleToCitizen);
            _reviews.Add(review);
        }

        private static string GenerateApplicationNumber()
        {
            var now = DateTime.UtcNow;
            var random = new Random();
            var randomDigits = random.Next(1000, 10000);
            return $"PERMIT-{now:yyyy}{now:MM}{now:dd}-{randomDigits}";
        }
    }

    public enum ReviewDecision
    {
        Approve = 1,
        Reject = 2,
        RequestInfo = 3
    }
}
