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
            var department = "IT";

            // Act
            var request = new CreateUserRequest
            {
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                Role = role,
                Department = department
            };

            // Assert
            Assert.Equal(email, request.Email);
            Assert.Equal(firstName, request.FirstName);
            Assert.Equal(lastName, request.LastName);
            Assert.Equal(role, request.Role);
            Assert.Equal(department, request.Department);
        }

        [Fact]
        public void Properties_WithNullDepartment_ShouldSetNullDepartment()
        {
            // Arrange & Act
            var request = new CreateUserRequest
            {
                Email = "test@example.com",
                FirstName = "John",
                LastName = "Doe",
                Role = "Citizen",
                Department = null
            };

            // Assert
            Assert.Null(request.Department);
        }     
    }
}
