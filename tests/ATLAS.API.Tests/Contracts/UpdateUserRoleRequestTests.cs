using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts
{
    public class UpdateUserRoleRequestTests
    {
        [Fact]
        public void Properties_ShouldBeSetCorrectly_WhenUsingObjectInitializer()
        {
            // Arrange
            var id = Guid.NewGuid();
            var role = "Admin";

            // Act
            var request = new UpdateUserRoleRequest
            {
                UserId = id,
                Role = role
            };

            // Assert
            Assert.Equal(id, request.UserId);
            Assert.Equal(role, request.Role);
        }     
    }
}
