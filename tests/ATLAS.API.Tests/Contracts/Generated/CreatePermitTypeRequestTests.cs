using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class CreatePermitTypeRequestTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var request = new CreatePermitTypeRequest
            {
                Name = "Building Permit",
                Description = "Permit for building construction",
                Fee = 150.00m
            };

            // Assert
            Assert.Equal("Building Permit", request.Name);
            Assert.Equal("Permit for building construction", request.Description);
            Assert.Equal(150.00m, request.Fee);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var request = new CreatePermitTypeRequest();

            // Assert
            Assert.Null(request.Name);
            Assert.Null(request.Description);
            Assert.Equal(default(decimal), request.Fee);
        }

        [Fact]
        public void AdditionalProperties_ShouldBeSettable()
        {
            // Arrange
            var request = new CreatePermitTypeRequest();
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
