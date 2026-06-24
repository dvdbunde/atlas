using System;
using System.Linq;
using Xunit;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Events;

namespace ATLAS.Domain.Tests.Entities
{
    public class ApplicationTests
    {
        private readonly Guid _citizenId = Guid.NewGuid();
        private readonly Guid _permitTypeId = Guid.NewGuid();
        private readonly Guid _officerId = Guid.NewGuid();

        [Fact]
        public void Create_ShouldInitializeWithDraftStatus()
        {
            // Arrange & Act
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Assert
            Assert.Equal(_citizenId, application.CitizenId);
            Assert.Equal(_permitTypeId, application.PermitTypeId);
            Assert.Equal("Test notes", application.CitizenNotes);
            Assert.Equal(ApplicationStatus.Draft, application.Status);
            Assert.Null(application.SubmittedDate);
            Assert.Null(application.ReviewedDate);
        }

        #region Submit Tests

        [Fact]
        public void Submit_ShouldTransitionFromDraftToSubmitted()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Act
            application.Submit();

            // Assert
            Assert.Equal(ApplicationStatus.Submitted, application.Status);
            Assert.NotNull(application.SubmittedDate);
            Assert.True(application.SubmittedDate <= DateTime.UtcNow);
        }

        [Fact]
        public void Submit_ShouldThrowException_WhenNotInDraftStatus()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit(); // Now it's Submitted

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => application.Submit());
            Assert.Equal("Only draft applications can be submitted", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenPermitTypeIdEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new Application(_citizenId, Guid.Empty, "Test notes"));
            Assert.Contains("Permit type ID cannot be empty", exception.Message);
        }

        [Fact]
        public void Submit_ShouldRaiseApplicationSubmittedEvent()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.ClearDomainEvents(); // Clear initial events

            // Act
            application.Submit();

            // Assert
            var domainEvent = Assert.Single(application.DomainEvents);
            var submittedEvent = Assert.IsType<ApplicationSubmittedEvent>(domainEvent);
            Assert.Equal(application.Id, submittedEvent.ApplicationId);
            Assert.Equal(_citizenId, submittedEvent.CitizenId);
            Assert.Equal(_permitTypeId, submittedEvent.PermitTypeId);
        }

        #endregion

        #region StartReview Tests

        [Fact]
        public void StartReview_ShouldTransitionFromSubmittedToUnderReview()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit();

            // Act
            application.StartReview(_officerId);

            // Assert
            Assert.Equal(ApplicationStatus.UnderReview, application.Status);
        }

        [Fact]
        public void StartReview_ShouldThrowException_WhenNotSubmitted()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => application.StartReview(_officerId));
            Assert.Contains("submitted applications", exception.Message);
        }

        [Fact]
        public void StartReview_ShouldRaiseApplicationUnderReviewEvent()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit();
            application.ClearDomainEvents();

            // Act
            application.StartReview(_officerId);

            // Assert
            var domainEvent = Assert.Single(application.DomainEvents);
            var reviewEvent = Assert.IsType<ApplicationUnderReviewEvent>(domainEvent);
            Assert.Equal(application.Id, reviewEvent.ApplicationId);
            Assert.Equal(_officerId, reviewEvent.OfficerId);
        }

        #endregion

        #region Approve Tests

        [Fact]
        public void Approve_ShouldTransitionFromUnderReviewToApproved()
        {
            // Arrange
            var application = CreateApplicationUnderReview();

            // Act
            application.Approve(_officerId, "Approved - meets all requirements");

            // Assert
            Assert.Equal(ApplicationStatus.Approved, application.Status);
            Assert.NotNull(application.ReviewedDate);
        }

        [Fact]
        public void Approve_ShouldThrowException_WhenNotUnderReview()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit();

            // Act & Assert - Status is Submitted, not UnderReview
            var exception = Assert.Throws<DomainException>(() => 
                application.Approve(_officerId, "Approved"));
            Assert.Contains("under review", exception.Message);
        }

        [Fact]
        public void Approve_ShouldRaiseApplicationApprovedEvent()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.ClearDomainEvents();

            // Act
            application.Approve(_officerId, "Approved");

            // Assert
            var domainEvent = Assert.Single(application.DomainEvents);
            var approvedEvent = Assert.IsType<ApplicationApprovedEvent>(domainEvent);
            Assert.Equal(application.Id, approvedEvent.ApplicationId);
            Assert.Equal(_officerId, approvedEvent.OfficerId);
        }

        #endregion

        #region Reject Tests

        [Fact]
        public void Reject_ShouldTransitionFromUnderReviewToRejected()
        {
            // Arrange
            var application = CreateApplicationUnderReview();

            // Act
            application.Reject(_officerId, "IncompleteApplication", "Missing documents");

            // Assert
            Assert.Equal(ApplicationStatus.Rejected, application.Status);
            Assert.NotNull(application.ReviewedDate);
        }

        [Fact]
        public void Reject_ShouldThrowException_WhenNotUnderReview()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                application.Reject(_officerId, "Reason", "Comments"));
            Assert.Contains("under review", exception.Message);
        }

        [Fact]
        public void Reject_ShouldThrowException_WhenReasonCodeEmpty()
        {
            // Arrange
            var application = CreateApplicationUnderReview();

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                application.Reject(_officerId, "", "Comments"));
            Assert.Contains("reason code", exception.Message);
        }

        [Fact]
        public void Reject_ShouldRaiseApplicationRejectedEvent()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.ClearDomainEvents();

            // Act
            application.Reject(_officerId, "IncompleteApplication", "Missing documents");

            // Assert
            var domainEvent = Assert.Single(application.DomainEvents);
            var rejectedEvent = Assert.IsType<ApplicationRejectedEvent>(domainEvent);
            Assert.Equal(application.Id, rejectedEvent.ApplicationId);
            Assert.Equal(_officerId, rejectedEvent.OfficerId);
            Assert.Equal("IncompleteApplication", rejectedEvent.ReasonCode);
        }

        [Fact]
        public void Reject_ShouldCreateReviewWithReasonCode()
        {
            // Arrange
            var application = CreateApplicationUnderReview();

            // Act
            application.Reject(_officerId, "IncompleteApplication", "Missing documents");

            // Assert
            var review = Assert.Single(application.Reviews);
            Assert.Equal("IncompleteApplication", review.ReasonCode);
            Assert.Equal(ReviewDecision.Reject, review.Decision);
        }

        #endregion

        #region RequestInfo Tests

        [Fact]
        public void RequestInfo_ShouldTransitionFromUnderReviewToInfoRequested()
        {
            // Arrange
            var application = CreateApplicationUnderReview();

            // Act
            application.RequestInfo(_officerId, "Please provide additional documentation");

            // Assert
            Assert.Equal(ApplicationStatus.InfoRequested, application.Status);
        }

        [Fact]
        public void RequestInfo_ShouldThrowException_WhenNotUnderReview()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                application.RequestInfo(_officerId, "Info"));
            Assert.Contains("under review", exception.Message);
        }

        [Fact]
        public void RequestInfo_ShouldRaiseApplicationInfoRequestedEvent()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.ClearDomainEvents();

            // Act
            application.RequestInfo(_officerId, "Please provide more info");

            // Assert
            var domainEvent = Assert.Single(application.DomainEvents);
            var infoEvent = Assert.IsType<ApplicationInfoRequestedEvent>(domainEvent);
            Assert.Equal(application.Id, infoEvent.ApplicationId);
            Assert.Equal(_officerId, infoEvent.OfficerId);
        }

        #endregion

        #region Resubmit Tests

        [Fact]
        public void Resubmit_ShouldTransitionFromInfoRequestedToUnderReview()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.RequestInfo(_officerId, "Please provide more info");

            // Act
            application.Resubmit();

            // Assert
            Assert.Equal(ApplicationStatus.UnderReview, application.Status);
        }

        [Fact]
        public void Resubmit_ShouldThrowException_WhenNotInfoRequested()
        {
            // Arrange
            var application = CreateApplicationUnderReview();

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => application.Resubmit());
            Assert.Contains("requested info", exception.Message);
        }

        [Fact]
        public void Resubmit_ShouldRaiseApplicationResubmittedEvent()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.RequestInfo(_officerId, "Please provide more info");
            application.ClearDomainEvents();

            // Act
            application.Resubmit();

            // Assert
            var domainEvent = Assert.Single(application.DomainEvents);
            var resubmittedEvent = Assert.IsType<ApplicationResubmittedEvent>(domainEvent);
            Assert.Equal(application.Id, resubmittedEvent.ApplicationId);
            Assert.Equal(_citizenId, resubmittedEvent.CitizenId);
        }

        #endregion

        #region Invalid State Transitions

        [Fact]
        public void Approve_ShouldThrowException_WhenDraft()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                application.Approve(_officerId, "Approved"));
            Assert.Contains("under review", exception.Message);
        }

        [Fact]
        public void Reject_ShouldThrowException_WhenDraft()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                application.Reject(_officerId, "Reason", "Comments"));
            Assert.Contains("under review", exception.Message);
        }

        [Fact]
        public void Submit_ShouldThrowException_WhenApproved()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.Approve(_officerId, "Approved");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => application.Submit());
            Assert.Contains("draft", exception.Message);
        }

        #endregion

        #region AddFieldValue Tests

        [Fact]
        public void AddFieldValue_ShouldAddFieldAndRaiseNoEvent()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.ClearDomainEvents();

            // Act
            application.AddFieldValue("PropertyAddress", "123 Main St", 0);

            // Assert
            var fieldValue = Assert.Single(application.FieldValues);
            Assert.Equal("PropertyAddress", fieldValue.FieldName);
            Assert.Equal("123 Main St", fieldValue.Value);
            Assert.Equal(0, fieldValue.SortOrder);
            Assert.Equal(application.Id, fieldValue.ApplicationId);
            // No domain events raised for field value operations
            Assert.Empty(application.DomainEvents);
        }

        [Fact]
        public void AddFieldValue_ShouldThrowException_WhenFieldNameIsEmpty()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() =>
                application.AddFieldValue("", "value", 0));
            Assert.Contains("Field name cannot be null, empty, or whitespace", exception.Message);
        }

        [Fact]
        public void AddFieldValue_ShouldMaintainSortOrder()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Act
            application.AddFieldValue("FieldA", "A", 5);
            application.AddFieldValue("FieldB", "B", 1);
            application.AddFieldValue("FieldC", "C", 10);

            //Assert
            Assert.Equal(3, application.FieldValues.Count);
            var fieldA = application.FieldValues.Single(fv => fv.FieldName == "FieldA");
            var fieldB = application.FieldValues.Single(fv => fv.FieldName == "FieldB");
            var fieldC = application.FieldValues.Single(fv => fv.FieldName == "FieldC");
            Assert.Equal(5, fieldA.SortOrder);
            Assert.Equal(1, fieldB.SortOrder);
            Assert.Equal(10, fieldC.SortOrder);
        }

        #endregion

        #region UpdateFieldValue Tests

        [Fact]
        public void UpdateFieldValue_ShouldUpdateExistingField()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.AddFieldValue("PropertyAddress", "Old value", 0);

            // Act
            application.UpdateFieldValue("PropertyAddress", "New value");

            // Assert
            Assert.Equal("New value", application.FieldValues[0].Value);
        }

        [Fact]
        public void UpdateFieldValue_ShouldThrowException_WhenFieldNotFound()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() =>
                application.UpdateFieldValue("NonExistent", "value"));
            Assert.Contains("not found", exception.Message);
        }

        #endregion

        #region RemoveFieldValue Tests

        [Fact]
        public void RemoveFieldValue_ShouldRemoveExistingField()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.AddFieldValue("FieldToRemove", "value", 0);
            application.AddFieldValue("FieldToKeep", "value", 1);

            // Act
            application.RemoveFieldValue("FieldToRemove");

            // Assert
            Assert.Single(application.FieldValues);
            Assert.Equal("FieldToKeep", application.FieldValues[0].FieldName);
        }

        [Fact]
        public void RemoveFieldValue_ShouldThrowException_WhenFieldNotFound()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() =>
                application.RemoveFieldValue("NonExistent"));
            Assert.Contains("not found", exception.Message);
        }

        #endregion

        #region AssignToOfficer Tests

        [Fact]
        public void AssignToOfficer_ShouldRaiseEvent_WhenSubmitted()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit();
            application.ClearDomainEvents();

            // Act
            application.AssignToOfficer(_officerId);

            // Assert
            var domainEvent = Assert.Single(application.DomainEvents);
            var assignEvent = Assert.IsType<ApplicationAssignedToOfficerEvent>(domainEvent);
            Assert.Equal(application.Id, assignEvent.ApplicationId);
            Assert.Equal(_officerId, assignEvent.OfficerId);
        }

        [Fact]
        public void AssignToOfficer_ShouldThrowException_WhenDraft()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() =>
                application.AssignToOfficer(_officerId));
            Assert.Contains("submitted or under-review", exception.Message);
        }

        [Fact]
        public void AssignToOfficer_ShouldThrowException_WhenApproved()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.Approve(_officerId, "Approved");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() =>
                application.AssignToOfficer(_officerId));
            Assert.Contains("submitted or under-review", exception.Message);
        }

        #endregion

        #region AddDocument Status Validation

        [Fact]
        public void AddDocument_ShouldThrowException_WhenApproved()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.Approve(_officerId, "Approved");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() =>
                application.AddDocument(Guid.NewGuid(), "doc.pdf", "application/pdf", 1024, "https://blob.url", _citizenId));
            Assert.Contains("Cannot add documents to approved or rejected applications", exception.Message);
        }

        [Fact]
        public void AddDocument_ShouldThrowException_WhenRejected()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.Reject(_officerId, "IncompleteApplication", "Missing documents");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() =>
                application.AddDocument(Guid.NewGuid(), "doc.pdf", "application/pdf", 1024, "https://blob.url", _citizenId));
            Assert.Contains("Cannot add documents to approved or rejected applications", exception.Message);
        }     
        #endregion

        #region AddReview Tests

        [Fact]
        public void AddReview_ShouldAddReviewSuccessfully()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            var reviewId = Guid.NewGuid();

            // Act
            application.AddReview(reviewId, _officerId, ReviewDecision.Approve, "Looks good", true, null);

            // Assert
            var review = Assert.Single(application.Reviews);
            Assert.Equal(reviewId, review.Id);
            Assert.Equal(_officerId, review.OfficerId);
            Assert.Equal(ReviewDecision.Approve, review.Decision);
        }

        [Fact]
        public void AddReview_ShouldThrowException_WhenDuplicateFinalReview()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.AddReview(Guid.NewGuid(), _officerId, ReviewDecision.Approve, "First", true, null);

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() =>
                application.AddReview(Guid.NewGuid(), _officerId, ReviewDecision.Reject, "Second", true, "MissingDoc"));
            Assert.Contains("already has a final review", exception.Message);
        }

        #endregion

        #region ApplicationNumber Tests

        [Fact]
        public void Create_ShouldGenerateValidApplicationNumber()
        {
            // Arrange & Act
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Assert
            Assert.StartsWith("PERMIT-", application.ApplicationNumber);
            Assert.Matches(@"^PERMIT-\d{8}-\d{4}$", application.ApplicationNumber);
        }

        #endregion

        #region Helper Methods

        private Application CreateApplicationUnderReview()
        {
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit();
            application.StartReview(_officerId);
            return application;
        }

        #endregion
    }
}
