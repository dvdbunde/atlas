using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class RejectApplicationRequestTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var request = new RejectApplicationRequest
            {
                ApplicationId = Guid.NewGuid(),
                ReasonCode = "CODE-001",
                Comments = "Missing documentation"
            };

            // Assert
            Assert.NotEqual(Guid.Empty, request.ApplicationId);
            Assert.Equal("CODE-001", request.ReasonCode);
            Assert.Equal("Missing documentation", request.Comments);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var request = new RejectApplicationRequest();

            // Assert
            Assert.Equal(default(Guid), request.ApplicationId);
            Assert.Null(request.ReasonCode); // Default is default! (null for string)
            Assert.Null(request.Comments); // Default is default! (null for string)
        }
    }
}
