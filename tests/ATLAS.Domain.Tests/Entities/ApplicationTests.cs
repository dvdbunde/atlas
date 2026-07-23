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
        }

        [Fact]
        public void Approve_ShouldCreateReview()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
        
            // Act
            application.Approve(_officerId, "Approved");
        
            // Assert
            var review = Assert.Single(application.Reviews);
            Assert.Equal(ReviewDecision.Approve, review.Decision);
            Assert.Equal(_officerId, review.OfficerId);
            Assert.True(review.IsVisibleToCitizen);
        }

        [Fact]
        public void Approve_ShouldThrow_WhenNotAssigned()
        {
            var a = CreateApplicationUnderReviewUnassigned(); a.ClearDomainEvents();
            Assert.Throws<DomainException>(() => a.Approve(_officerId, "ok"));
        }

        [Fact]
        public void Approve_ShouldThrow_WhenAssignedToOtherOfficer()
        {
            var a = CreateApplicationUnderReviewUnassigned(); a.AssignToOfficer(Guid.NewGuid());
            Assert.Throws<DomainException>(() => a.Approve(_officerId, "ok"));
        }

        [Fact]
        public void Approve_WhenAssignedToCurrentOfficer_ShouldSucceed()
        {
            var a = CreateApplicationUnderReview(); a.ClearDomainEvents();
            a.Approve(_officerId, "ok");
            Assert.Equal(ApplicationStatus.Approved, a.Status);
            Assert.Single(a.Reviews);
            Assert.IsType<ApplicationApprovedEvent>(Assert.Single(a.DomainEvents));
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

        [Fact]
        public void Reject_ShouldThrow_WhenNotAssigned()
        {
            var a = CreateApplicationUnderReviewUnassigned(); a.ClearDomainEvents();
            Assert.Throws<DomainException>(() => a.Reject(_officerId, "INCOMPLETE", "ok"));
        }

        [Fact]
        public void Reject_ShouldThrow_WhenAssignedToOtherOfficer()
        {
            var a = CreateApplicationUnderReviewUnassigned(); a.AssignToOfficer(Guid.NewGuid());
            Assert.Throws<DomainException>(() => a.Reject(_officerId, "INCOMPLETE", "ok"));
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
        }

        [Fact]
        public void RequestInfo_ShouldCreateReview()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
        
            // Act
            application.RequestInfo(_officerId, "Please provide additional documentation");
        
            // Assert
            var review = Assert.Single(application.Reviews);
            Assert.Equal(ReviewDecision.RequestInfo, review.Decision);
            Assert.Equal(_officerId, review.OfficerId);
            Assert.True(review.IsVisibleToCitizen);
        }

        [Fact]
        public void RequestInfo_ShouldThrow_WhenNotAssigned()
        {
            var a = CreateApplicationUnderReviewUnassigned(); a.ClearDomainEvents();
            Assert.Throws<DomainException>(() => a.RequestInfo(_officerId, "need more"));
        }

        [Fact]
        public void RequestInfo_ShouldThrow_WhenAssignedToOtherOfficer()
        {
            var a = CreateApplicationUnderReviewUnassigned(); a.AssignToOfficer(Guid.NewGuid());
            Assert.Throws<DomainException>(() => a.RequestInfo(_officerId, "need more"));
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
        }

        [Fact]
        public void Resubmit_ShouldPreserveAssignment()
        {
            var application = CreateApplicationUnderReview();
            var assignedOfficerId = application.AssignedOfficerId;
            application.RequestInfo(_officerId, "Need more info");

            application.Resubmit();

            Assert.Equal(assignedOfficerId, application.AssignedOfficerId);
            Assert.Equal(ApplicationStatus.UnderReview, application.Status);
        }

        [Fact]
        public void Resubmit_ShouldPreserveReviewHistory()
        {
            var application = CreateApplicationUnderReview();
            application.RequestInfo(_officerId, "Need survey");

            application.Resubmit();

            Assert.NotEmpty(application.Reviews);
            Assert.Contains(application.Reviews, r => r.Decision == ReviewDecision.RequestInfo);
        }

        [Fact]
        public void Resubmit_ShouldThrow_WhenNotInfoRequested()
        {
            var application = CreateApplicationUnderReview();
            // Status is UnderReview, not InfoRequested
            Assert.Throws<DomainException>(() => application.Resubmit());
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

        #region Duplicate Decision / Review History Integrity

        [Fact]
        public void Approve_ShouldThrow_WhenAlreadyApproved()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.Approve(_officerId, "First approval");

            // Act & Assert — status is now Approved, not UnderReview
            var exception = Assert.Throws<DomainException>(() =>
                application.Approve(_officerId, "Second approval"));
            Assert.Contains("under review", exception.Message);
        }

        [Fact]
        public void Reject_ShouldThrow_WhenAlreadyRejected()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.Reject(_officerId, "INCOMPLETE", "Missing docs");

            // Act & Assert — status is now Rejected, not UnderReview
            var exception = Assert.Throws<DomainException>(() =>
                application.Reject(_officerId, "INCOMPLETE", "Again"));
            Assert.Contains("under review", exception.Message);
        }

        [Fact]
        public void RequestInfo_ShouldThrow_WhenAlreadyInfoRequested()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.RequestInfo(_officerId, "First request");

            // Act & Assert — status is now InfoRequested, not UnderReview
            var exception = Assert.Throws<DomainException>(() =>
                application.RequestInfo(_officerId, "Second request"));
            Assert.Contains("under review", exception.Message);
        }

        [Fact]
        public void Decision_ShouldPreserveReviewHistory()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.RequestInfo(_officerId, "Please provide survey");

            // Act — citizen resubmits, then officer approves
            application.Resubmit();
            application.Approve(_officerId, "Approved after resubmission");

            // Assert — both the RequestInfo review and the Approve review are retained
            Assert.Equal(2, application.Reviews.Count);
            Assert.Contains(application.Reviews, r => r.Decision == ReviewDecision.RequestInfo);
            Assert.Contains(application.Reviews, r => r.Decision == ReviewDecision.Approve);
            Assert.Equal(ApplicationStatus.Approved, application.Status);
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
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit();
            application.ClearDomainEvents();

            application.AssignToOfficer(_officerId);

            // Both UnderReview and Assigned events are raised
            Assert.Equal(2, application.DomainEvents.Count);
            Assert.Contains(application.DomainEvents, e => e is ApplicationUnderReviewEvent);
            Assert.Contains(application.DomainEvents, e => e is ApplicationAssignedToOfficerEvent);
            Assert.Equal(ApplicationStatus.UnderReview, application.Status);
            Assert.Equal(_officerId, application.AssignedOfficerId);
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

        [Fact]
        public void NewApplication_ShouldStartUnassigned()
        {
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            Assert.Null(application.AssignedOfficerId);
            Assert.Null(application.AssignedDate);
        }

        [Fact]
        public void AssignToOfficer_ShouldSetAssignedDate()
        {
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit();
            application.ClearDomainEvents();

            application.AssignToOfficer(_officerId);

            Assert.Equal(_officerId, application.AssignedOfficerId);
            Assert.NotNull(application.AssignedDate);
            Assert.True(application.AssignedDate <= DateTime.UtcNow);
            Assert.True(application.AssignedDate >= DateTime.UtcNow.AddMinutes(-1));
        }

        [Fact]
        public void AssignToOfficer_SameOfficer_ShouldBeIdempotent_NoDateReset_NoEvent()
        {
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit();
            application.AssignToOfficer(_officerId);
            var firstDate = application.AssignedDate;
            application.ClearDomainEvents();

            application.AssignToOfficer(_officerId); // retry

            Assert.Equal(_officerId, application.AssignedOfficerId);
            Assert.Equal(firstDate, application.AssignedDate); // not reset
            Assert.Empty(application.DomainEvents);            // no duplicate event
        }

        [Fact]
        public void AssignToOfficer_OtherOfficer_ShouldThrowAndKeepOriginal()
        {
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit();
            application.AssignToOfficer(_officerId);
            var originalDate = application.AssignedDate;
            application.ClearDomainEvents();
            var other = Guid.NewGuid();

            var ex = Assert.Throws<DomainException>(() => application.AssignToOfficer(other));
            Assert.Contains("already assigned to another officer", ex.Message);

            Assert.Equal(_officerId, application.AssignedOfficerId); // unchanged
            Assert.Equal(originalDate, application.AssignedDate);    // unchanged
            Assert.Empty(application.DomainEvents);                  // no event
        }

        [Fact]
        public void AssignToOfficer_ShouldThrow_WhenInfoRequested()
        {
            var application = CreateApplicationUnderReview();
            application.RequestInfo(_officerId, "Need more info");

            var ex = Assert.Throws<DomainException>(() => application.AssignToOfficer(_officerId));
            Assert.Contains("submitted or under-review", ex.Message);
        }

        [Fact]
        public void AssignToOfficer_ShouldThrow_WhenEmptyOfficerId()
        {
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit();

            Assert.Throws<ArgumentException>(() => application.AssignToOfficer(Guid.Empty));
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
                application.AddDocument(Guid.NewGuid(), "Building Permit","doc.pdf", "application/pdf", 1024, "https://blob.url", _citizenId));
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
                application.AddDocument(Guid.NewGuid(),"Building Permit", "doc.pdf", "application/pdf", 1024, "https://blob.url", _citizenId));
            Assert.Contains("Cannot add documents to approved or rejected applications", exception.Message);
        }     
        #endregion

        #region AddReview Tests

        [Fact]
        public void AddReview_ShouldAddReviewSuccessfully()
        {
            // Arrange
            var reviewId = Guid.NewGuid();
            var application = new Application(_citizenId, _permitTypeId, "Test notes");            

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
            application.AssignToOfficer(_officerId); // O4: decisions require assignment
            return application;
        }

        private Application CreateApplicationUnderReviewUnassigned()
        {
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            application.Submit();
            application.StartReview(_officerId);
            return application;
        }

        #endregion

        #region Document Management Tests

        [Fact]
        public void AddDocument_ShouldAddDocumentToList()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            var documentId = Guid.NewGuid();

            // Act
            var document = application.AddDocument(
                documentId,
                "Building Permit",
                "test.pdf",
                "application/pdf",
                1024,
                "https://blob.url/test.pdf",
                _citizenId);

            // Assert
            Assert.Single(application.Documents);
            Assert.Equal(documentId, document.Id);
            Assert.Equal("test.pdf", document.FileName);
        }

        [Fact]
        public void AddDocument_ShouldThrow_WhenApplicationIsApproved()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.Approve(_officerId, "Approved");

            // Act & Assert
            Assert.Throws<DomainException>(() =>
                application.AddDocument(
                    Guid.NewGuid(),
                    "ParkingPermit",
                    "test.pdf",
                    "application/pdf",
                    1024,
                    "https://blob.url/test.pdf",
                    _citizenId));
        }

        [Fact]
        public void AddDocument_ShouldThrow_WhenApplicationIsRejected()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            application.Reject(_officerId, "INCOMPLETE", "Missing documents");

            // Act & Assert
            Assert.Throws<DomainException>(() =>
                application.AddDocument(
                    Guid.NewGuid(),
                    "ParkingPermit",
                    "test.pdf",
                    "application/pdf",
                    1024,
                    "https://blob.url/test.pdf",
                    _citizenId));
        }

        [Fact]
        public void RemoveDocument_ShouldRemoveDocumentFromList()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            var documentId = Guid.NewGuid();
            application.AddDocument(
                documentId,
                "ParkingPermit",
                "test.pdf",
                "application/pdf",
                1024,
                "https://blob.url/test.pdf",
                _citizenId);

            // Act
            application.RemoveDocument(documentId);

            // Assert
            Assert.Empty(application.Documents);
        }

        [Fact]
        public void RemoveDocument_ShouldThrow_WhenDocumentNotFound()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");

            // Act & Assert
            Assert.Throws<DomainException>(() =>
                application.RemoveDocument(Guid.NewGuid()));
        }

        [Fact]
        public void RemoveDocument_ShouldThrow_WhenApplicationIsApproved()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            var documentId = Guid.NewGuid();
            application.AddDocument(
                documentId,
                "ParkingPermit",
                "test.pdf",
                "application/pdf",
                1024,
                "https://blob.url/test.pdf",
                _citizenId);
            application.ClearDomainEvents();
            application.Approve(_officerId, "Approved");

            // Act & Assert
            Assert.Throws<DomainException>(() =>
                application.RemoveDocument(documentId));
        }

        [Fact]
        public void RemoveDocument_ShouldThrow_WhenApplicationIsRejected()
        {
            // Arrange
            var application = CreateApplicationUnderReview();
            var documentId = Guid.NewGuid();
            application.AddDocument(
                documentId,
                "ParkingPermit",
                "test.pdf",
                "application/pdf",
                1024,
                "https://blob.url/test.pdf",
                _citizenId);
            application.ClearDomainEvents();
            application.Reject(_officerId, "INCOMPLETE", "Missing docs");

            // Act & Assert
            Assert.Throws<DomainException>(() =>
                application.RemoveDocument(documentId));
        }

        [Fact]
        public void MultipleDocuments_ShouldAllBeAccessible()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Test notes");
            var doc1Id = Guid.NewGuid();
            var doc2Id = Guid.NewGuid();

            // Act
            application.AddDocument(doc1Id, "ParkingPermit", "doc1.pdf", "application/pdf", 1024, "https://blob.url/1", _citizenId);
            application.AddDocument(doc2Id, "ParkingPermit", "doc2.pdf", "application/pdf", 2048, "https://blob.url/2", _citizenId);

            // Assert
            Assert.Equal(2, application.Documents.Count);
            Assert.Contains(application.Documents, d => d.Id == doc1Id);
            Assert.Contains(application.Documents, d => d.Id == doc2Id);
        }

        #endregion
    }
}
