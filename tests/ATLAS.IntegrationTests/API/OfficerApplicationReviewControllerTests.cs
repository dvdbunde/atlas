using ATLAS.API.Contracts.Generated;
using ATLAS.IntegrationTests.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace ATLAS.IntegrationTests.API
{
    [Collection("Sequential Integration Tests")]
    public class OfficerApplicationReviewControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public OfficerApplicationReviewControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _client = factory.CreateClient();
            _output = output;
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };

        [Fact]
        public async Task GetOfficerApplicationReview_Should_Return200_ForOfficer()
        {
            var applicationId = TestData.Application1Id;
            var response = await _client.GetAsAsync(
                $"/api/applications/officer/{applicationId}", TestUserBuilder.AsOfficer());

            _output.WriteLine($"Status: {response.StatusCode}");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var review = await response.Content.ReadFromJsonAsync<OfficerApplicationReviewResponse>(_jsonOptions);
            Assert.NotNull(review);
            Assert.Equal(applicationId, review.ApplicationId);
        }

        [Fact]
        public async Task GetOfficerApplicationReview_Should_Return403_ForCitizen()
        {
            var applicationId = TestData.Application1Id;
            var response = await _client.GetAsAsync(
                $"/api/applications/officer/{applicationId}", TestUserBuilder.AsCitizen());

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetOfficerApplicationReview_Should_Return401_ForAnonymous()
        {
            var applicationId = TestData.Application1Id;
            var response = await _client.GetAnonymousAsync(
                $"/api/applications/officer/{applicationId}");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetOfficerApplicationReview_Should_Return200_ForAdmin()
        {
            var applicationId = TestData.Application1Id;
            var response = await _client.GetAsAsync(
                $"/api/applications/officer/{applicationId}", TestUserBuilder.AsAdmin());

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetOfficerApplicationReview_Should_Return404_ForUnknownApplication()
        {
            var response = await _client.GetAsAsync(
                $"/api/applications/officer/{Guid.NewGuid()}", TestUserBuilder.AsOfficer());

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetOfficerApplicationReview_Should_NotExposeBlobUrl()
        {
            var applicationId = TestData.Application1Id;
            var response = await _client.GetAsAsync(
                $"/api/applications/officer/{applicationId}", TestUserBuilder.AsOfficer());

            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync();
            Assert.DoesNotContain("blob", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("BlobUrl", body);
        }
    }
}