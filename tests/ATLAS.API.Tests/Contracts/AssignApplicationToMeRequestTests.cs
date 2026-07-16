using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts
{
    public class AssignApplicationToMeRequestTests
    {
        [Fact]
        public void Properties_ShouldBeSetCorrectly_WhenUsingObjectInitializer()
        {
            // Arrange
            var applicationId = Guid.NewGuid();

            // Act
            var request = new AssignApplicationToMeRequest
            {
                ApplicationId = applicationId
            };

            // Assert
            Assert.Equal(applicationId, request.ApplicationId);
        }
    }
}
