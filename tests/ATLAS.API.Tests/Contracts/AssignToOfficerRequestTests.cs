using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts
{
    public class AssignToOfficerRequestTests
    {
        [Fact]
        public void Properties_ShouldBeSetCorrectly_WhenUsingObjectInitializer()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var officerId = Guid.NewGuid();

            // Act
            var request = new AssignToOfficerRequest
            {
                ApplicationId = applicationId,
                OfficerId = officerId
            };

            // Assert
            Assert.Equal(applicationId, request.ApplicationId);
            Assert.Equal(officerId, request.OfficerId);
        }    
    }
}
