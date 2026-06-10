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
                OfficerId = Guid.NewGuid(),
                ReasonCode = "CODE-001",
                Comments = "Missing documentation"
            };

            // Assert
            Assert.NotEqual(Guid.Empty, request.ApplicationId);
            Assert.NotEqual(Guid.Empty, request.OfficerId);
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
            Assert.Equal(default(Guid), request.OfficerId);
            Assert.Null(request.ReasonCode); // Default is default! (null for string)
            Assert.Null(request.Comments); // Default is default! (null for string)
        }
    }
}
