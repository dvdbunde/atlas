using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class ApplicationSummaryResponseTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var response = new ApplicationSummaryResponse
            {
                Id = Guid.NewGuid(),
                ApplicationNumber = "APP-2024-001",
                Status = 1,
                SubmittedDate = DateTimeOffset.UtcNow,
                CitizenId = Guid.NewGuid(),
                PermitTypeId = Guid.NewGuid(),
                CitizenName = "John Doe",
                PermitTypeName = "Building Permit"
            };

            // Assert
            Assert.NotEqual(Guid.Empty, response.Id);
            Assert.Equal("APP-2024-001", response.ApplicationNumber);
            Assert.Equal(1, response.Status);
            Assert.NotNull(response.SubmittedDate);
            Assert.NotEqual(Guid.Empty, response.CitizenId);
            Assert.NotEqual(Guid.Empty, response.PermitTypeId);
            Assert.Equal("John Doe", response.CitizenName);
            Assert.Equal("Building Permit", response.PermitTypeName);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var response = new ApplicationSummaryResponse();

            // Assert
            Assert.Equal(default(Guid), response.Id);
            Assert.Null(response.ApplicationNumber);
            Assert.Equal(0, response.Status);
            Assert.Null(response.SubmittedDate);
            Assert.Equal(default(Guid), response.CitizenId);
            Assert.Equal(default(Guid), response.PermitTypeId);
            Assert.Null(response.CitizenName);
            Assert.Null(response.PermitTypeName);
        }

        [Fact]
        public void AdditionalProperties_ShouldBeSettable()
        {
            // Arrange
            var response = new ApplicationSummaryResponse();
            var additionalProps = new Dictionary<string, object>
            {
                { "customField", "customValue" }
            };

            // Act
            response.AdditionalProperties = additionalProps;

            // Assert
            Assert.NotNull(response.AdditionalProperties);
            Assert.Equal("customValue", ((Dictionary<string, object>)response.AdditionalProperties)["customField"]);
        }
    }
}
