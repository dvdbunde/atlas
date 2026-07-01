using ATLAS.API.Contracts.Generated;
using System;
using Xunit;

namespace ATLAS.API.Tests.Contracts.Generated
{
    public class DocumentResponseTests
    {
        [Fact]
        public void PropertyInitialization_ShouldSetValuesCorrectly()
        {
            // Arrange & Act
            var response = new DocumentResponse
            {
                Id = Guid.NewGuid(),
                FileName = "permit.pdf",
                ContentType = "application/pdf",
                FileSize = 1024000,                
                UploadedDate = DateTimeOffset.UtcNow
            };

            // Assert
            Assert.NotEqual(Guid.Empty, response.Id);
            Assert.Equal("permit.pdf", response.FileName);
            Assert.Equal("application/pdf", response.ContentType);
            Assert.Equal(1024000, response.FileSize);            
            Assert.NotNull(response.UploadedDate);
        }

        [Fact]
        public void DefaultValues_ShouldBeSetCorrectly()
        {
            // Arrange & Act
            var response = new DocumentResponse();

            // Assert
            Assert.Equal(default(Guid), response.Id);
            Assert.Null(response.FileName);
            Assert.Null(response.ContentType);
            Assert.Equal(0, response.FileSize);            
            Assert.Equal(default(DateTimeOffset), response.UploadedDate);
        }
    }
}
