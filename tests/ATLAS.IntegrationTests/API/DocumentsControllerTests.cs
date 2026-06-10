using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using Xunit;
using Xunit.Abstractions;

namespace ATLAS.IntegrationTests.API
{
    public class DocumentsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        private readonly ITestOutputHelper _output;

        public DocumentsControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _client = factory.CreateClient();
            _output = output;
        }
        
        [Fact]
        public async Task UploadDocument_Should_ReturnNoContent()
        {
            // Arrange - Use seeded application ID
            var applicationId = TestData.Application1Id;
            var request = new
            {
                applicationId = applicationId.ToString(),
                fileName = "test.pdf",
                contentType = "application/pdf",
                fileSize = 1024,
                blobUrl = "https://example.com/test.pdf",
                uploadedById = TestData.CitizenUserId
            };
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json");

            // Act
            var response = await _client.PostAsync($"/api/applications/{applicationId}/documents", content);            

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task DownloadDocument_Should_Return501NotImplemented()
        {
            // Arrange - Use seeded document ID
            var documentId = TestData.Document1Id;

            // Act
            var response = await _client.GetAsync($"/api/documents/{documentId}/download");

            // Assert
            Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        }
    }
}
