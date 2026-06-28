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
    public class DocumentsControllerTests : IClassFixture<CustomWebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;

        public DocumentsControllerTests(CustomWebApplicationFactory<Program> factory, ITestOutputHelper output)
        {
            _client = factory.CreateClientWithoutRedirects();
            _output = output;
        }
                
        [Fact]
        public async Task UploadDocument_Should_ReturnNoContent()
        {
            var applicationId = TestData.Application2Id;
            var request = new
            {
                applicationId = applicationId.ToString(),
                fileName = "aaa.pdf",
                contentType = "application/pdf",
                fileSize = 1024,
                fileContent = Convert.ToBase64String(new byte[] { 0x25, 0x50, 0x44, 0x46 }) // PDF magic bytes as base64
            };
            var response = await _client.PostAsAsync($"/api/applications/{applicationId}/documents", request, TestUserBuilder.AsCitizen());
            _output.WriteLine($"Response: {response.StatusCode}");
            _output.WriteLine($"Response Content: {await response.Content.ReadAsStringAsync()}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
        
        [Fact]
        public async Task UploadThenDownloadDocument_EndToEnd()
        {
            var applicationId = TestData.Application2Id;

            // Arrange — a small PDF payload
            var uploadRequest = new
            {
                applicationId = applicationId.ToString(),
                fileName = "test.pdf",
                contentType = "application/pdf",
                fileSize = 1024,
                fileContent = Convert.ToBase64String(new byte[] { 0x25, 0x50, 0x44, 0x46 })
            };

            // Act 1 — Upload the document
            var uploadResponse = await _client.PostAsAsync(
                $"/api/applications/{applicationId}/documents",
                uploadRequest,
                TestUserBuilder.AsCitizen());

            _output.WriteLine($"Upload Response: {uploadResponse.StatusCode}");
            Assert.Equal(HttpStatusCode.NoContent, uploadResponse.StatusCode);

            // Act 2 — Get the application detail to retrieve the new document's ID
            var detailResponse = await _client.GetAsAsync(
                $"/api/applications/{applicationId}",
                TestUserBuilder.AsCitizen());

            _output.WriteLine($"Detail Response: {detailResponse.StatusCode}");
            detailResponse.EnsureSuccessStatusCode();

            var detail = await detailResponse.Content.ReadFromJsonAsync<ApplicationDetailResponse>(_jsonOptions);
            Assert.NotNull(detail);
            Assert.NotEmpty(detail.Documents);

            // Last document should be the one we just uploaded
            var uploadedDoc = detail.Documents.Last();
            Assert.Equal("test.pdf", uploadedDoc.FileName);

            // Act 3 — Download the document
            var downloadResponse = await _client.GetAsAsync(
                $"/api/documents/{uploadedDoc.Id}/download",
                TestUserBuilder.AsCitizen());

            _output.WriteLine($"Download Response: {downloadResponse.StatusCode}");
            _output.WriteLine($"Response Content: {await downloadResponse.Content.ReadAsStringAsync()}");

            // Assert — 302 redirect with a Location header (SAS URI)
            Assert.Equal(HttpStatusCode.Redirect, downloadResponse.StatusCode);
            Assert.NotNull(downloadResponse.Headers.Location);
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };                               
    }
}
