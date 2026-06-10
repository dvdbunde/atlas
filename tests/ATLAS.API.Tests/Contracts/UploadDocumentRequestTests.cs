using ATLAS.API.Contracts.Generated;
using Xunit;

namespace ATLAS.API.Tests.Contracts
{
    public class UploadDocumentRequestTests
    {
        [Fact]
        public void Properties_ShouldBeSetCorrectly_WhenUsingObjectInitializer()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            var fileName = "test.pdf";
            var contentType = "application/pdf";
            var fileSize = 1024L;
            var blobUrl = new Uri("https://example.com/test.pdf");

            // Act
            var request = new UploadDocumentRequest
            {
                ApplicationId = applicationId,
                FileName = fileName,
                ContentType = contentType,
                FileSize = fileSize,
                BlobUrl = blobUrl
            };

            // Assert
            Assert.Equal(applicationId, request.ApplicationId);
            Assert.Equal(fileName, request.FileName);
            Assert.Equal(contentType, request.ContentType);
            Assert.Equal(fileSize, request.FileSize);
            Assert.Equal(blobUrl, request.BlobUrl);
        }      
    }
}
