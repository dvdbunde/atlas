using System;
using ATLAS.Domain.Entities;
using Xunit;

namespace ATLAS.Domain.Tests.Entities
{
    public class DocumentTests
    {
        private readonly Guid _documentId = Guid.NewGuid();
        private readonly Guid _applicationId = Guid.NewGuid();
        private readonly Guid _uploadedById = Guid.NewGuid();

        [Fact]
        public void Create_ShouldInitializeWithValidValues()
        {
            // Arrange & Act
            var document = new Document(
                _documentId, 
                _applicationId, 
                "application.pdf", 
                "application/pdf", 
                1024 * 1024, // 1MB
                "https://storage.blob.core.windows.net/documents/app.pdf",
                _uploadedById);

            // Assert
            Assert.Equal(_documentId, document.Id);
            Assert.Equal(_applicationId, document.ApplicationId);
            Assert.Equal("application.pdf", document.FileName);
            Assert.Equal("application/pdf", document.ContentType);
            Assert.Equal(1024 * 1024, document.FileSize);
            Assert.Equal("https://storage.blob.core.windows.net/documents/app.pdf", document.BlobUrl);
            Assert.Equal(_uploadedById, document.UploadedById);
            Assert.True(document.UploadedDate <= DateTime.UtcNow);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenIdIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new Document(
                    Guid.Empty, 
                    _applicationId, 
                    "file.pdf", 
                    "application/pdf", 
                    1024, 
                    "https://blob.url",
                    _uploadedById));
            Assert.Contains("Document ID cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenFileNameIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new Document(
                    _documentId, 
                    _applicationId, 
                    "", 
                    "application/pdf", 
                    1024, 
                    "https://blob.url",
                    _uploadedById));
            Assert.Contains("File name cannot be empty", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenFileNameTooLong()
        {
            // Arrange
            var longFileName = new string('A', 300); // 300 chars

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new Document(
                    _documentId, 
                    _applicationId, 
                    longFileName, 
                    "application/pdf", 
                    1024, 
                    "https://blob.url",
                    _uploadedById));
            Assert.Contains("cannot exceed 255 characters", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenFileSizeExceeds25MB()
        {
            // Arrange
            var largeFileSize = 26L * 1024 * 1024; // 26MB

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new Document(
                    _documentId, 
                    _applicationId, 
                    "file.pdf", 
                    "application/pdf", 
                    largeFileSize, 
                    "https://blob.url",
                    _uploadedById));
            Assert.Contains("cannot exceed 25MB", exception.Message);
        }

        [Fact]
        public void Create_ShouldThrowException_WhenBlobUrlIsEmpty()
        {
            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                new Document(
                    _documentId, 
                    _applicationId, 
                    "file.pdf", 
                    "application/pdf", 
                    1024, 
                    "", 
                    _uploadedById));
            Assert.Contains("Blob URL cannot be empty", exception.Message);
        }
    }
}
