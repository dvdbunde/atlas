using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using Xunit;

namespace ATLAS.IntegrationTests.API
{
    public class AuditLogsControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public AuditLogsControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetAuditLogs_Should_Return200OK()
        {
            // Act
            var response = await _client.GetAsync("/api/auditlogs");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAuditLogs_WithUserId_Should_Return200OK()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/auditlogs?userId={userId}");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetAuditLogs_WithActionType_Should_Return200OK()
        {
            // Act
            var response = await _client.GetAsync("/api/auditlogs?actionType=APPLICATION_SUBMITTED");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ExportAuditLogs_Should_Return501NotImplemented()
        {
            // Act
            var response = await _client.GetAsync("/api/auditlogs/export");

            // Assert
            Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        }

        [Fact]
        public async Task ExportAuditLogs_WithFilters_Should_Return501NotImplemented()
        {
            // Arrange
            var userId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/auditlogs/export?userId={userId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        }
    }
}
