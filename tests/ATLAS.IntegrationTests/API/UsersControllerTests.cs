using ATLAS.IntegrationTests.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using ATLAS.IntegrationTests;

namespace ATLAS.IntegrationTests.API
{
    public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public UsersControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetUsers_Should_Return200OK()
        {
            // Act
            var response = await _client.GetAsAsync("/api/users", TestUserBuilder.AsAdmin());

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetUsers_WithRole_Should_Return200OK()
        {
            // Act
            var response = await _client.GetAsAsync("/api/users?role=Admin", TestUserBuilder.AsAdmin());

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
      
        [Fact]
        public async Task GetUserById_WithValidId_Should_Return200OK()
        {
            // Arrange - Use seeded citizen ID
            var userId = TestData.CitizenUserId;

            // Act
            var response = await _client.GetAsAsync($"/api/users/{userId}", TestUserBuilder.AsAdmin());

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetUserById_WithInvalidId_Should_Return404NotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsAsync($"/api/users/{userId}", TestUserBuilder.AsAdmin());

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AuthenticatedRequest_ShouldSyncExistingUser()
        {
            // Arrange & Act - Use seeded Admin identity; sync happens via pipeline
            var response = await _client.GetAsAsync("/api/users", TestUserBuilder.AsAdmin());

            // Assert - Request succeeded; sync ran without error
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AuthenticatedRequest_WithNewIdentity_ShouldCreateUser()
        {
            // Arrange - Use a new user identity that won't match any seeded Domain User
            // The fallback identity will create a new Domain User via IdentityResolver
            var newUserId = Guid.NewGuid();
            var newUserBuilder = TestUserBuilder.AsUser(newUserId, "New Sync User", "test@atlas.test", "Admin");

            // Act - First request triggers UserSynchronizationBehavior
            var firstResponse = await _client.GetAsAsync("/api/users", newUserBuilder);
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

            // Verify the synced user appears in the user list
            var listResponse = await _client.GetAsAsync("/api/users", newUserBuilder);
            var content = await listResponse.Content.ReadAsStringAsync();

            // Assert - The new user with email "test@atlas.test" should have been created
            Assert.Contains("test@atlas.test", content);
        }
    }
}
