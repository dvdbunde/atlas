using ATLAS.IntegrationTests.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
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

        /*  
        [Fact]
        public async Task UploadDocument_Should_ReturnNoContent()
        {
            var applicationId = TestData.Application4Id;
            var request = new
            {
                applicationId = applicationId.ToString(),
                fileName = "aaa.pdf",
                contentType = "application/pdf",
                fileSize = 1024,
                fileContent = Convert.ToBase64String(new byte[] { 0x25, 0x50, 0x44, 0x46 }) // PDF magic bytes as base64
            };
            var response = await _client.PostAsAsync($"/api/applications/{applicationId}/documents", request, TestUserBuilder.AsCitizen());
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }
        */

        [Fact]
        public async Task DownloadDocument_Should_Return501NotImplemented()
        {
            var documentId = TestData.Document1Id;
            var response = await _client.GetAsAsync($"/api/documents/{documentId}/download", TestUserBuilder.AsAdmin());
            Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
        }
    }
}
