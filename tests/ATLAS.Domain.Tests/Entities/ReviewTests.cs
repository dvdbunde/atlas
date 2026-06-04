using System;
using ATLAS.Domain.Entities;
using Xunit;

namespace ATLAS.Domain.Tests.Entities
{
    public class ReviewTests
    {
        private readonly Guid _reviewId = Guid.NewGuid();
        private readonly Guid _applicationId = Guid.NewGuid();
        private readonly Guid _officerId = Guid.NewGuid();

        [Fact]
        public void Create_ShouldInitializeWithValidValues()
        {
            // Arrange & Act
            var application = new Application(Guid.NewGuid(), Guid.NewGuid(), "Initial notes");
            var review = application.AddReview(
                _reviewId,                 
                _officerId, 
                ReviewDecision.Approve, 
                "Approved - meets all requirements", 
                true);

            // Assert
            Assert.Equal(_reviewId, review.Id);
            Assert.Equal(_applicationId, review.ApplicationId);
            Assert.Equal(_officerId, review.OfficerId);
            Assert.Equal(ReviewDecision.Approve, review.Decision);
            Assert.Equal("Approved - meets all requirements", review.Comments);
            Assert.True(review.IsVisibleToCitizen);
            Assert.True(review.ReviewedDate <= DateTime.UtcNow);
        }

        [Fact]
        public void Create_ShouldSetReasonCode_WhenDecisionIsReject()
        {
            // Arrange & Act
            var application = new Application(Guid.NewGuid(), Guid.NewGuid(), "Initial notes");
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
            // Act & Assert
            var application = new Application(Guid.NewGuid(), Guid.NewGuid(), "Initial notes");
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
        public void Create_ShouldThrowException_WhenApplicationIdIsEmpty()
        {
            // Act & Assert
            var application = new Application(Guid.NewGuid(), Guid.NewGuid(), "Initial notes");
            var exception = Assert.Throws<ArgumentException>(() => 
                application.AddReview(
                    _reviewId,                     
                    _officerId, 
                    ReviewDecision.Approve, 
                    "Approved", 
                    true));
            Assert.Contains("Application ID cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenOfficerIdIsEmpty()
        {
            // Act & Assert
            var application = new Application(Guid.NewGuid(), Guid.NewGuid(), "Initial notes");
            var exception = Assert.Throws<ArgumentException>(() => 
                application.AddReview(
                    _reviewId, 
                    Guid.Empty, 
                    ReviewDecision.Approve, 
                    "Approved", 
                    true));
            Assert.Contains("Officer ID cannot be empty", exception.Message);
        }
    }
}
