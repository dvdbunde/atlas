using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class ApproveApplicationRequestTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var request = new ApproveApplicationRequest
            {
                ApplicationId = Guid.NewGuid(),
                OfficerId = Guid.NewGuid(),
                Comments = "Approved after review"
            };

            // Assert
            Assert.NotEqual(Guid.Empty, request.ApplicationId);
            Assert.NotEqual(Guid.Empty, request.OfficerId);
            Assert.Equal("Approved after review", request.Comments);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var request = new ApproveApplicationRequest();

            // Assert
            Assert.Equal(default(Guid), request.ApplicationId);
            Assert.Equal(default(Guid), request.OfficerId);
            Assert.Equal("", request.Comments); // Default is ""
        }
    }
}
