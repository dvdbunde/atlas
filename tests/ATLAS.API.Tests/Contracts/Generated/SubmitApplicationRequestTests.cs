using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class SubmitApplicationRequestTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var request = new SubmitApplicationRequest
            {
                CitizenId = Guid.NewGuid(),
                PermitTypeId = Guid.NewGuid(),
                CitizenNotes = "Need permit for renovation"
            };

            // Assert
            Assert.NotEqual(Guid.Empty, request.CitizenId);
            Assert.NotEqual(Guid.Empty, request.PermitTypeId);
            Assert.Equal("Need permit for renovation", request.CitizenNotes);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var request = new SubmitApplicationRequest();

            // Assert
            Assert.Equal(default(Guid), request.CitizenId);
            Assert.Equal(default(Guid), request.PermitTypeId);
            Assert.Equal("", request.CitizenNotes); // Default is ""
        }

        [Fact]
        public void AdditionalProperties_ShouldBeSettable()
        {
            // Arrange
            var request = new SubmitApplicationRequest();
            var additionalProps = new Dictionary<string, object>
            {
                { "customField", "customValue" }
            };

            // Act
            request.AdditionalProperties = additionalProps;

            // Assert
            Assert.NotNull(request.AdditionalProperties);
            Assert.Equal("customValue", ((Dictionary<string, object>)request.AdditionalProperties)["customField"]);
        }
    }
}
