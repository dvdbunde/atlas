using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts
{
    public class RequestInfoRequestTests
    {
        [Fact]
        public void Properties_ShouldBeSetCorrectly_WhenUsingObjectInitializer()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var officerId = Guid.NewGuid();            
            var message = "Please provide additional documentation";

            // Act
            var request = new RequestInfoRequest
            {
                ApplicationId = applicationId,
                OfficerId = officerId,
                Message = message
            };

            // Assert
            Assert.Equal(applicationId, request.ApplicationId);
            Assert.Equal(officerId, request.OfficerId);
            Assert.Equal(message, request.Message);
        }      
    }
}
