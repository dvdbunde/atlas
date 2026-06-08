using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using Xunit;

namespace ATLAS.IntegrationTests.API
{
    public class UsersControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public UsersControllerTests(WebApplicationFactory<Program> factory)
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
            // Arrange
            var userId = Guid.NewGuid();

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
            // Arrange
            var userId = Guid.NewGuid();
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
    }
}
