using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using Xunit;

namespace ATLAS.IntegrationTests.API
{
    public class ApplicationsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public ApplicationsControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetApplications_Should_Return200OK()
        {
            // Act
            var response = await _client.GetAsync("/api/applications");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetApplications_WithCitizenId_Should_Return200OK()
        {
            // Arrange - Use seeded citizen ID
            var citizenId = TestData.CitizenUserId;

            // Act
            var response = await _client.GetAsync($"/api/applications?citizenId={citizenId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task SubmitApplication_Should_Return201Created()
        {
            // Arrange - Use seeded data
            var request = new
            {
                citizenId = TestData.CitizenUserId,
                permitTypeId = TestData.BuildingPermitTypeId,
                citizenNotes = "Test application"
            };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync("/api/applications", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task GetApplicationById_WithValidId_Should_Return200OK()
        {
            // Arrange - Use seeded application ID
            var applicationId = TestData.Application1Id;

            // Act
            var response = await _client.GetAsync($"/api/applications/{applicationId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetApplicationById_WithInvalidId_Should_Return404NotFound()
        {
            // Arrange - Use non-existent ID
            var applicationId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/applications/{applicationId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ApproveApplication_Should_Return200OK()
        {
            // Arrange - Use seeded application and officer IDs
            var applicationId = TestData.Application1Id;
            var request = new { officerId = TestData.OfficerUserId, comments = "Approved" };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync($"/api/applications/{applicationId}/approve", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RejectApplication_Should_Return200OK()
        {
            // Arrange - Use seeded application (submitted, can be rejected)
            var applicationId = TestData.Application2Id;
            var request = new { reasonCode = "INCOMPLETE_DOCUMENTATION", comments = "Rejected" };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync($"/api/applications/{applicationId}/reject", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task RequestInfo_Should_Return200OK()
        {
            // Arrange - Use seeded application (submitted, can request info)
            var applicationId = TestData.Application2Id;
            var request = new { officerId = TestData.OfficerUserId, message = "Please provide additional information" };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync($"/api/applications/{applicationId}/request-info", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task AssignToOfficer_Should_Return200OK()
        {
            // Arrange - Use seeded application (not submitted yet)
            var applicationId = TestData.Application3Id;
            var request = new { officerId = TestData.OfficerUserId };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync($"/api/applications/{applicationId}/assign", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
