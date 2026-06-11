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
            var response = await _client.GetAsync("/api/users");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetUsers_WithRole_Should_Return200OK()
        {
            // Act
            var response = await _client.GetAsync("/api/users?role=Admin");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreateUser_Should_Return201Created()
        {
            // Arrange
            var request = new
            {
                email = "test@example.com",
                firstName = "Test",
                lastName = "User",
                role = "Citizen"
            };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/users", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task GetUserById_WithValidId_Should_Return200OK()
        {
            // Arrange - Use seeded citizen ID
            var userId = TestData.CitizenUserId;

            // Act
            var response = await _client.GetAsync($"/api/users/{userId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetUserById_WithInvalidId_Should_Return404NotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/users/{userId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateUserRole_Should_Return200OK()
        {
            // Arrange - Use seeded officer ID
            var userId = TestData.OfficerUserId;
            var request = new { role = "Officer" };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PutAsync($"/api/users/{userId}/role", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AuthenticatedRequest_ShouldSyncExistingUser()
        {
            // Arrange - Use seeded Admin identity which has known UserId
            var previousRole = TestData.CurrentTestRole;
            TestData.CurrentTestRole = "Admin";

            try
            {
                // Act - Make an authenticated request that triggers UserSynchronizationBehavior
                var response = await _client.GetAsync("/api/users");

                // Assert - Request succeeded; sync ran without error
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
            finally
            {
                TestData.CurrentTestRole = previousRole;
            }
        }

        [Fact]
        public async Task AuthenticatedRequest_WithNewIdentity_ShouldCreateUser()
        {
            // Arrange - Use a role that triggers the TestAuthHandler's default fallback identity
            // The fallback identity has a hardcoded GUID and email "test@atlas.test" that
            // won't match any seeded user, so IdentityResolver will create a new Domain User.
            var previousRole = TestData.CurrentTestRole;
            TestData.CurrentTestRole = "NewSyncTestUser";

            try
            {
                // Act - First request triggers UserSynchronizationBehavior
                var firstResponse = await _client.GetAsync("/api/users");
                Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

                // Verify the synced user appears in the user list
                var listResponse = await _client.GetAsync("/api/users");
                var content = await listResponse.Content.ReadAsStringAsync();

                // Assert - The new user with email "test@atlas.test" should have been created
                Assert.Contains("test@atlas.test", content);
            }
            finally
            {
                TestData.CurrentTestRole = previousRole;
            }
        }
    }
}
