using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class PermitTypeResponseTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var response = new PermitTypeResponse
            {
                Id = Guid.NewGuid(),
                Name = "Building Permit",
                Description = "Permit for building construction",
                Fee = 150.00m,
                IsActive = true
            };

            // Assert
            Assert.NotEqual(Guid.Empty, response.Id);
            Assert.Equal("Building Permit", response.Name);
            Assert.Equal("Permit for building construction", response.Description);
            Assert.Equal(150.00m, response.Fee);
            Assert.True(response.IsActive);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var response = new PermitTypeResponse();

            // Assert
            Assert.Equal(default(Guid), response.Id);
            Assert.Null(response.Name);
            Assert.Null(response.Description);
            Assert.Equal(default(decimal), response.Fee);
            Assert.True(response.IsActive); // Default is true
        }
    }
}
