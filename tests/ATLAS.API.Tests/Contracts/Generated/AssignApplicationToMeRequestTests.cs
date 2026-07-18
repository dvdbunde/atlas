using ATLAS.API.Contracts.Generated;
using System.Collections.Generic;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class AssignApplicationToMeRequestTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var request = new AssignApplicationToMeRequest
            {
                ApplicationId = Guid.NewGuid()
            };

            // Assert
            Assert.NotEqual(Guid.Empty, request.ApplicationId);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var request = new AssignApplicationToMeRequest();

            // Assert
            Assert.Equal(default(Guid), request.ApplicationId);
        }

        [Fact]
        public void AdditionalProperties_ShouldBeSettable()
        {
            // Arrange
            var request = new AssignApplicationToMeRequest();
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
