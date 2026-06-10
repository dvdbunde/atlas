using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class UserResponseTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var response = new UserResponse
            {
                Id = Guid.NewGuid(),
                Email = "john.doe@example.com",
                FirstName = "John",
                LastName = "Doe",
                Role = "Citizen",
                Department = "IT",
                IsActive = true
            };

            // Assert
            Assert.NotEqual(Guid.Empty, response.Id);
            Assert.Equal("john.doe@example.com", response.Email);
            Assert.Equal("John", response.FirstName);
            Assert.Equal("Doe", response.LastName);
            Assert.Equal("Citizen", response.Role);
            Assert.Equal("IT", response.Department);
            Assert.True(response.IsActive);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var response = new UserResponse();

            // Assert
            Assert.Equal(default(Guid), response.Id);
            Assert.Null(response.Email);
            Assert.Null(response.FirstName);
            Assert.Null(response.LastName);
            Assert.Null(response.Role);
            Assert.Null(response.Department);
            Assert.True(response.IsActive); // Default is true
        }
    }
}
