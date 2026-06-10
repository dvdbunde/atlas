using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class ProblemDetailsTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var problemDetails = new ProblemDetails
            {
                Title = "Bad Request",
                Status = 400,
                Detail = "Invalid input",
                Instance = "https://api.example.com/error"
            };

            // Assert
            Assert.Equal("Bad Request", problemDetails.Title);
            Assert.Equal(400, problemDetails.Status);
            Assert.Equal("Invalid input", problemDetails.Detail);
            Assert.Equal("https://api.example.com/error", problemDetails.Instance);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var problemDetails = new ProblemDetails();

            // Assert
            Assert.Null(problemDetails.Title);
            Assert.Equal(default(int), problemDetails.Status);
            Assert.Null(problemDetails.Detail);
            Assert.Null(problemDetails.Instance);
        }

        [Fact]
        public void AdditionalProperties_ShouldBeSettable()
        {
            // Arrange
            var problemDetails = new ProblemDetails();
            var additionalProps = new Dictionary<string, object>
            {
                { "customField", "customValue" }
            };

            // Act
            problemDetails.AdditionalProperties = additionalProps;

            // Assert
            Assert.NotNull(problemDetails.AdditionalProperties);
            Assert.Equal("customValue", ((Dictionary<string, object>)problemDetails.AdditionalProperties)["customField"]);
        }
    }
}
