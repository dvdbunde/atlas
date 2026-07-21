using ATLAS.IntegrationTests.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace ATLAS.IntegrationTests.API
{
    [Collection("Sequential Integration Tests")]
    public class AuditLogsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public AuditLogsControllerTests(CustomWebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAuditLogs_Should_Return200OK()
        {
            var response = await _client.GetAsAsync("/api/auditlogs", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAuditLogs_WithUserId_Should_Return200OK()
        {
            var userId = Guid.NewGuid();
            var response = await _client.GetAsAsync($"/api/auditlogs?userId={userId}", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAuditLogs_WithActionType_Should_Return200OK()
        {
            var response = await _client.GetAsAsync("/api/auditlogs?action=APPLICATION_SUBMITTED", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ExportAuditLogs_Should_Return501NotImplemented()
        {
            var response = await _client.GetAsAsync("/api/auditlogs/export", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        }

        [Fact]
        public async Task ExportAuditLogs_WithFilters_Should_Return501NotImplemented()
        {
            var userId = Guid.NewGuid();
            var response = await _client.GetAsAsync($"/api/auditlogs/export?userId={userId}", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        }

        [Fact]
        public async Task GetAuditLogs_Should_ReturnPagedResponse()
        {
            var response = await _client.GetAsAsync("/api/auditlogs?pageNumber=1&pageSize=10", TestUserBuilder.AsAdmin());
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("\"totalCount\"", content);
            Assert.Contains("\"items\"", content);
            Assert.Contains("\"totalPages\"", content);
        }

        [Fact]
        public async Task GetAuditLogById_WithUnknownId_Should_Return404()
        {
            var id = Guid.NewGuid();
            var response = await _client.GetAsAsync($"/api/auditlogs/{id}", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetAuditLogs_WithoutAdminRole_Should_Return403()
        {
            var response = await _client.GetAsAsync("/api/auditlogs", TestUserBuilder.AsCitizen());
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }    }
}

