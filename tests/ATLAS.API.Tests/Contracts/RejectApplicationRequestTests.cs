using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts
{
    public class RejectApplicationRequestTests
    {
        [Fact]
        public void Properties_ShouldBeSetCorrectly_WhenUsingObjectInitializer()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var officerId = Guid.NewGuid();
            var reasonCode = "INCOMPLETE";
            var comments = "Missing documents";

            // Act
            var request = new RejectApplicationRequest
            {
                ApplicationId = applicationId,
                OfficerId = officerId,
                ReasonCode = reasonCode,
                Comments = comments
            };

            // Assert
            Assert.Equal(applicationId, request.ApplicationId);
            Assert.Equal(officerId, request.OfficerId);
            Assert.Equal(reasonCode, request.ReasonCode);
            Assert.Equal(comments, request.Comments);
        }      
    }
}
