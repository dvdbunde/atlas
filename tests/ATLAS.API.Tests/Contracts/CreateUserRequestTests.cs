using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts
{
    public class CreateUserRequestTests
    {
        [Fact]
        public void Properties_ShouldBeSetCorrectly_WhenUsingObjectInitializer()
        {
            // Arrange
            var email = "test@example.com";
            var firstName = "John";
            var lastName = "Doe";
            var role = "Citizen";

            // Act
            var request = new CreateUserRequest
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = role
            };

            // Assert
            Assert.Equal(email, request.Email);
            Assert.Equal(firstName, request.FirstName);
            Assert.Equal(lastName, request.LastName);
            Assert.Equal(role, request.Role);
        }     
    }
}
