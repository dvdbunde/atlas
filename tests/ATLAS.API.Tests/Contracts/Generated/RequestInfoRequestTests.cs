using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class RequestInfoRequestTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var request = new RequestInfoRequest
            {
                ApplicationId = Guid.NewGuid(),
                Message = "Please provide additional documentation"
            };

            // Assert
            Assert.NotEqual(Guid.Empty, request.ApplicationId);
            Assert.Equal("Please provide additional documentation", request.Message);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var request = new RequestInfoRequest();

            // Assert
            Assert.Equal(default(Guid), request.ApplicationId);
            Assert.Equal(default(string), request.Message);
        }
    }
}
