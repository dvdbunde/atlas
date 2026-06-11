using ATLAS.IntegrationTests.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
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
            var response = await _client.GetAsAsync("/api/permittypes", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetPermitTypes_WithIncludeInactive_Should_Return200OK()
        {
            var response = await _client.GetAsAsync("/api/permittypes?includeInactive=true", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task CreatePermitType_Should_Return201Created()
        {
            var request = new
            {
                name = "Test Permit Type",
                description = "Test description",
                fee = 100.00m
            };
            var response = await _client.PostAsAsync("/api/permittypes", request, TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task GetPermitTypeById_WithValidId_Should_Return200OK()
        {
            var permitTypeId = TestData.BuildingPermitTypeId;
            var response = await _client.GetAsAsync($"/api/permittypes/{permitTypeId}", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetPermitTypeById_WithInvalidId_Should_Return404NotFound()
        {
            var permitTypeId = Guid.NewGuid();
            var response = await _client.GetAsAsync($"/api/permittypes/{permitTypeId}", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdatePermitType_Should_Return200OK()
        {
            var permitTypeId = TestData.BuildingPermitTypeId;
            var request = new
            {
                permitTypeId = permitTypeId.ToString(),
                name = "Updated Permit Type",
                description = "Updated description",
                isActive = true,
                estimatedProcessingDays = 5
            };
            var response = await _client.PutAsAsync($"/api/permittypes/{permitTypeId}", request, TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DeactivatePermitType_Should_ReturnNoContent()
        {
            var permitTypeId = TestData.BuildingPermitTypeId;
            var response = await _client.DeleteAsAsync($"/api/permittypes/{permitTypeId}", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}
