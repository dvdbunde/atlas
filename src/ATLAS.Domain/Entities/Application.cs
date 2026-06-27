using System;
using System.Collections.Generic;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Events;

namespace ATLAS.Domain.Entities
{
    /// <summary>
    /// Application aggregate root - enforces invariants for Application and its child entities.
    /// 
    /// AGGREGATE BOUNDARIES (Phase A - Milestone 5):
    /// - Application is the aggregate root
    /// - Documents are owned by Application (OwnsMany)
    /// - Reviews are owned by Application (OwnsMany)
    /// - FieldValues are owned by Application (OwnsMany)
    /// 
    /// DOMAIN INVARIANTS:
    /// 1. FieldValues collection follows same ownership model as Documents and Reviews
    /// 2. No separate repository for FieldValues - persisted through Application
    /// 3. FieldName references PermitField.Name (immutable reference)
    /// 4. Required fields must be populated before submission (validated in Application layer)
    /// </summary>
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

        private readonly List<ApplicationFieldValue> _fieldValues = new();
        public IReadOnlyList<ApplicationFieldValue> FieldValues => _fieldValues.AsReadOnly();

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

        /// <summary>
        /// Updates the citizen notes for this application.
        /// Only allowed when the application is in Draft or InfoRequested status.
        /// </summary>
        /// <param name="notes">The new notes text (null becomes empty string).</param>
        /// <exception cref="DomainException">Thrown when the application is not in an editable state.</exception>
        public void UpdateNotes(string notes)
        {
            if (Status != ApplicationStatus.Draft && Status != ApplicationStatus.InfoRequested)
                throw new DomainException("Can only update notes for draft or info-requested applications");

            CitizenNotes = notes ?? string.Empty;
        }

        public void Submit()
        {
            if (Status != ApplicationStatus.Draft)
                throw new DomainException("Only draft applications can be submitted");

            if (PermitTypeId == Guid.Empty)
                throw new DomainException("Cannot submit application without a permit type");

            // Note: Actual permit type validation (exists, is active) happens in command handler
            // Domain layer shouldn't have infrastructure dependencies

            Status = ApplicationStatus.Submitted;
            SubmittedDate = DateTime.UtcNow;
            AddDomainEvent(new ApplicationSubmittedEvent(Id, CitizenId, PermitTypeId));
        }

        public void StartReview(Guid officerId)
        {
            if (Status != ApplicationStatus.Submitted)
                throw new DomainException("Can only start review for submitted applications");

            Status = ApplicationStatus.UnderReview;            
            AddDomainEvent(new ApplicationUnderReviewEvent(Id, officerId));
        }

        public void AssignToOfficer(Guid officerId)
        {
            if (Status != ApplicationStatus.Submitted && Status != ApplicationStatus.UnderReview)
                throw new DomainException("Can only assign officer to submitted or under-review applications");

            // Note: Officer assignment is tracked via Review entities
            // This method validates the state transition is valid
            AddDomainEvent(new ApplicationAssignedToOfficerEvent(Id, officerId));
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
            
            // Create review with reason code
            AddReview(officerId, ReviewDecision.Reject, comments, true, reasonCode);
            
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

        public void Resubmit()
        {
            if (Status != ApplicationStatus.InfoRequested)
                throw new DomainException("Can only resubmit applications that have requested info");

            Status = ApplicationStatus.UnderReview;
            AddDomainEvent(new ApplicationResubmittedEvent(Id, CitizenId));
        }

        public Document AddDocument(Guid documentId, string fileName, string contentType, long fileSize, string blobUrl, Guid uploadedById)
        {
            if (Status == ApplicationStatus.Approved || Status == ApplicationStatus.Rejected)
                throw new DomainException("Cannot add documents to approved or rejected applications");

            var document = new Document(documentId, Id, fileName, contentType, fileSize, blobUrl, uploadedById);
            _documents.Add(document);           
            return document;
        }

        public ApplicationFieldValue AddFieldValue(string fieldName, string value, int sortOrder)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field name cannot be null, empty, or whitespace", nameof(fieldName));

            if (_fieldValues.Any(fv => fv.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase)))
                throw new DomainException($"Field '{fieldName}' already exists in this application");

            var fieldValue = new ApplicationFieldValue(Id, fieldName, value, sortOrder);
            _fieldValues.Add(fieldValue);

            return fieldValue;
        }

        public void UpdateFieldValue(string fieldName, string newValue)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field name cannot be null, empty, or whitespace", nameof(fieldName));

            var fieldValue = _fieldValues.FirstOrDefault(fv => fv.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            
            if (fieldValue == null)
                throw new DomainException($"Field '{fieldName}' not found in this application");

            fieldValue.UpdateValue(newValue);
        }

        public void RemoveFieldValue(string fieldName)
        {
            if (string.IsNullOrWhiteSpace(fieldName))
                throw new ArgumentException("Field name cannot be null, empty, or whitespace", nameof(fieldName));

            var fieldValue = _fieldValues.FirstOrDefault(fv => fv.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
            
            if (fieldValue == null)
                throw new DomainException($"Field '{fieldName}' not found in this application");

            _fieldValues.Remove(fieldValue);
        }

        public Review AddReview(Guid officerId, ReviewDecision decision, string comments, bool isVisibleToCitizen, string reasonCode = null)
        {
            // Note: Status check is done by the calling method (Submit, Approve, Reject, etc.)
            // This allows internal calls from Reject() before status changes to Rejected

            // Invariant: Only one active review at a time
            if (_reviews.Any(r => r.Decision == ReviewDecision.Approve || r.Decision == ReviewDecision.Reject))
                throw new DomainException("Application already has a final review");

            var review = new Review(Id, officerId, decision, comments, isVisibleToCitizen, reasonCode);
            _reviews.Add(review);

            return review;
        }
        

        private static string GenerateApplicationNumber()
        {
            var now = DateTime.UtcNow;
            var random = new Random();
            var randomDigits = random.Next(1000, 10000);
            return $"PERMIT-{now:yyyy}{now:MM}{now:dd}-{randomDigits}";
        }
    }
}
