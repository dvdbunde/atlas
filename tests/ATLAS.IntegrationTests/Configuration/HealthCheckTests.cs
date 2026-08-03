//----------------------
// Health Check Integration Tests
// Verifies /health/live and /health/ready endpoints respond correctly
//----------------------

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ATLAS.IntegrationTests.Configuration
{
    [Collection("Sequential Integration Tests")]
    public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly CustomWebApplicationFactory<Program> _factory;

        public HealthCheckTests(CustomWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _factory.ResetDatabase();
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Health_Liveness_Returns200()
        {
            var response = await _client.GetAsync("/health/live");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Health_Liveness_ReturnsValidJson()
        {
            var response = await _client.GetAsync("/health/live");
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(json.TryGetProperty("status", out var status));
            Assert.True(json.TryGetProperty("totalDuration", out var duration));
            Assert.Equal("Healthy", status.GetString());
        }

        [Fact]
        public async Task Health_Readiness_Returns200()
        {
            var response = await _client.GetAsync("/health/ready");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Health_Readiness_ReturnsValidJson()
        {
            var response = await _client.GetAsync("/health/ready");
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();

            Assert.True(json.TryGetProperty("status", out var status));
            Assert.True(json.TryGetProperty("totalDuration", out var duration));
            // In testing environment no Azure dependencies are configured,
            // so readiness reports Healthy as well
            Assert.Equal("Healthy", status.GetString());
        }

        [Fact]
        public async Task Health_Endpoints_DoNotRequireAuthentication()
        {
            var liveResponse = await _client.GetAsync("/health/live");
            var readyResponse = await _client.GetAsync("/health/ready");

            Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, readyResponse.StatusCode);
        }
    }
}
