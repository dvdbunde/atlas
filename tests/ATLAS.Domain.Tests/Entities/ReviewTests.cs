using System;
using ATLAS.Domain.Entities;
using Xunit;

namespace ATLAS.Domain.Tests.Entities
{
    public class ReviewTests
    {
        private readonly Guid _reviewId = Guid.NewGuid();
        private readonly Guid _citizenId = Guid.NewGuid();
        private readonly Guid _permitTypeId = Guid.NewGuid();
        private readonly Guid _officerId = Guid.NewGuid();

        private Application CreateApplicationUnderReview()
        {
            var application = new Application(_citizenId, _permitTypeId, "Initial notes");
            application.Submit();
            application.StartReview(_officerId);
            return application;
        }

        [Fact]
        public void Create_ShouldInitializeWithValidValues()
        {
            // Arrange
            var application = CreateApplicationUnderReview();

            // Act
            var review = application.AddReview(
                _reviewId,                 
                _officerId, 
                ReviewDecision.Approve, 
                "Approved - meets all requirements", 
                true);

            // Assert
            Assert.Equal(_reviewId, review.Id);
            Assert.Equal(application.Id, review.ApplicationId);
            Assert.Equal(_officerId, review.OfficerId);
            Assert.Equal(ReviewDecision.Approve, review.Decision);
            Assert.Equal("Approved - meets all requirements", review.Comments);
            Assert.True(review.IsVisibleToCitizen);
            Assert.True(review.ReviewedDate <= DateTime.UtcNow);
        }

        [Fact]
        public void Create_ShouldSetReasonCode_WhenDecisionIsReject()
        {
            // Arrange
            var application = CreateApplicationUnderReview();

            // Act
            var review = application.AddReview(
                _reviewId, 
                _officerId, 
                ReviewDecision.Reject,                 
                "Missing required documents", 
                false);

            // Assert
            Assert.Equal(ReviewDecision.Reject, review.Decision);
            Assert.Equal("IncompleteApplication", review.ReasonCode);
            Assert.False(review.IsVisibleToCitizen);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenIdIsEmpty()
        {
            // Arrange
            var application = CreateApplicationUnderReview();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                application.AddReview(
                    Guid.Empty, 
                    _officerId, 
                    ReviewDecision.Approve, 
                    "Approved", 
                    true));
            Assert.Contains("Review ID cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenOfficerIdIsEmpty()
        {
            // Arrange
            var application = CreateApplicationUnderReview();

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                application.AddReview(
                    _reviewId, 
                    Guid.Empty, 
                    ReviewDecision.Approve, 
                    "Approved", 
                    true));
            Assert.Contains("Officer ID cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenNotUnderReview()
        {
            // Arrange
            var application = new Application(_citizenId, _permitTypeId, "Initial notes");

            // Act & Assert
            var exception = Assert.Throws<DomainException>(() => 
                application.AddReview(
                    _reviewId, 
                    _officerId, 
                    ReviewDecision.Approve, 
                    "Approved", 
                    true));
            Assert.Contains("Can only add reviews for applications under review", exception.Message);
        }
    }
}
