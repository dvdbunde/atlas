using ATLAS.API.Contracts.Generated;
using System;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class UploadDocumentRequestTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var request = new UploadDocumentRequest
            {
                ApplicationId = Guid.NewGuid(),
                FileName = "permit.pdf",
                ContentType = "application/pdf",
                FileSize = 1024000,
                BlobUrl = new Uri("https://storage.blob.core.windows.net/documents/permit.pdf")
            };

            // Assert
            Assert.NotEqual(Guid.Empty, request.ApplicationId);
            Assert.Equal("permit.pdf", request.FileName);
            Assert.Equal("application/pdf", request.ContentType);
            Assert.Equal(1024000, request.FileSize);
            Assert.NotNull(request.BlobUrl);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var request = new UploadDocumentRequest();

            // Assert
            Assert.Equal(default(Guid), request.ApplicationId);
            Assert.Null(request.FileName);
            Assert.Null(request.ContentType);
            Assert.Equal(0, request.FileSize);
            Assert.Null(request.BlobUrl);
        }
    }
}
