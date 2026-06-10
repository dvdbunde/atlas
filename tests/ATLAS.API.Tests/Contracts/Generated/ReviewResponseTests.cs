using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class ReviewResponseTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var response = new ReviewResponse
            {
                Id = Guid.NewGuid(),
                OfficerId = Guid.NewGuid(),
                Decision = ReviewResponseDecision._1,
                ReasonCode = "CODE-001",
                Comments = "Approved",
                ReviewedDate = DateTimeOffset.UtcNow,
                IsVisibleToCitizen = true
            };

            // Assert
            Assert.NotEqual(Guid.Empty, response.Id);
            Assert.NotEqual(Guid.Empty, response.OfficerId);
            Assert.Equal(ReviewResponseDecision._1, response.Decision);
            Assert.Equal("CODE-001", response.ReasonCode);
            Assert.Equal("Approved", response.Comments);
            Assert.NotNull(response.ReviewedDate);
            Assert.True(response.IsVisibleToCitizen);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var response = new ReviewResponse();

            // Assert
            Assert.Equal(default(Guid), response.Id);
            Assert.Equal(default(Guid), response.OfficerId);
            Assert.Equal(default(ReviewResponseDecision), response.Decision);
            Assert.Null(response.ReasonCode);
            Assert.Null(response.Comments);
            Assert.Equal(default(DateTimeOffset), response.ReviewedDate);
            Assert.False(response.IsVisibleToCitizen); // Default is false
        }

        [Fact]
        public void Decision_ShouldSupportBothValues()
        {
            // Arrange & Act
            var response1 = new ReviewResponse { Decision = ReviewResponseDecision._1 };
            var response2 = new ReviewResponse { Decision = ReviewResponseDecision._2 };

            // Assert
            Assert.Equal(ReviewResponseDecision._1, response1.Decision);
            Assert.Equal(ReviewResponseDecision._2, response2.Decision);
        }
    }
}
