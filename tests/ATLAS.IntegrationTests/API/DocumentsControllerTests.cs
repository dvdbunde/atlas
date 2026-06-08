using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using Xunit;

namespace ATLAS.IntegrationTests.API
{
    public class DocumentsControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public DocumentsControllerTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task UploadDocument_Should_Return200OK()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var request = new
            {
                applicationId = applicationId.ToString(),
                fileName = "test.pdf",
                contentType = "application/pdf",
                fileSize = 1024,
                blobUrl = "https://example.com/test.pdf",
                uploadedById = Guid.NewGuid()
            };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync($"/api/applications/{applicationId}/documents", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DownloadDocument_Should_Return501NotImplemented()
        {
            // Arrange
            var documentId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/documents/{documentId}/download");

            // Assert
            Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        }
    }
}
