using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts
{
    public class ApproveApplicationRequestTests
    {
        [Fact]
        public void Properties_ShouldBeSetCorrectly_WhenUsingObjectInitializer()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var comments = "Approved";

            // Act
            var request = new ApproveApplicationRequest
            {
                ApplicationId = applicationId,
                Comments = comments
            };

            // Assert
            Assert.Equal(applicationId, request.ApplicationId);
            Assert.Equal(comments, request.Comments);
        }    
    }
}
