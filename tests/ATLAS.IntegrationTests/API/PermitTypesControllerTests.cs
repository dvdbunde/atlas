using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using Xunit;
using ATLAS.IntegrationTests;

namespace ATLAS.IntegrationTests.API
{
    public class PermitTypesControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public PermitTypesControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetPermitTypes_Should_Return200OK()
        {
            // Act
            var response = await _client.GetAsync("/api/permittypes");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetPermitTypes_WithIncludeInactive_Should_Return200OK()
        {
            // Act
            var response = await _client.GetAsync("/api/permittypes?includeInactive=true");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreatePermitType_Should_Return201Created()
        {
            // Arrange
            var request = new
            {
                name = "Test Permit Type",
                description = "Test description",
                fee = 100.00m
            };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/permittypes", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task GetPermitTypeById_WithValidId_Should_Return200OK()
        {
            // Arrange - Use seeded permit type ID
            var permitTypeId = TestData.BuildingPermitTypeId;

            // Act
            var response = await _client.GetAsync($"/api/permittypes/{permitTypeId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetPermitTypeById_WithInvalidId_Should_Return404NotFound()
        {
            // Arrange
            var permitTypeId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/permittypes/{permitTypeId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePermitType_Should_Return200OK()
        {
            // Arrange - Use seeded permit type ID
            var permitTypeId = TestData.BuildingPermitTypeId;
            var request = new
            {
                permitTypeId = permitTypeId.ToString(),
                name = "Updated Permit Type",
                description = "Updated description",
                isActive = true,
                estimatedProcessingDays = 5
            };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PutAsync($"/api/permittypes/{permitTypeId}", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DeactivatePermitType_Should_ReturnNoContent()
        {
            // Arrange - Use seeded permit type ID
            var permitTypeId = TestData.BuildingPermitTypeId;

            // Act
            var response = await _client.DeleteAsync($"/api/permittypes/{permitTypeId}");

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
