//----------------------
// BlobStorageService Tests
// Tests for the production Azure Blob Storage service implementation
// Uses InMemoryFileStorageService to avoid real Azure dependency during tests
//----------------------

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Infrastructure.Services;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Services
{
    public class FileStorageServiceContractTests
    {
        private readonly InMemoryFileStorageService _service;

        public FileStorageServiceContractTests()
        {
            _service = new InMemoryFileStorageService();
        }

        [Fact]
        public async Task UploadAsync_ShouldStoreFileAndReturnResult()
        {
            // Arrange
            var content = "Test file content";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            const string fileName = "test.pdf";
            const string contentType = "application/pdf";

            // Act
            var result = await _service.UploadAsync(stream, fileName, contentType);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.BlobUrl);
            Assert.True(result.Size > 0);
            Assert.Contains(fileName, result.BlobUrl);
        }

        [Fact]
        public async Task UploadAsync_ShouldThrowArgumentNullException_WhenStreamIsNull()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _service.UploadAsync(null!, "test.pdf", "application/pdf"));
        }

        [Fact]
        public async Task UploadAsync_ShouldThrowArgumentException_WhenFileNameIsEmpty()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadAsync(stream, "", "application/pdf"));
        }

        [Fact]
        public async Task UploadAsync_ShouldGenerateUniqueBlobUrls()
        {
            // Arrange
            var content1 = new MemoryStream(Encoding.UTF8.GetBytes("File 1"));
            var content2 = new MemoryStream(Encoding.UTF8.GetBytes("File 2"));

            // Act
            var result1 = await _service.UploadAsync(content1, "file1.pdf", "application/pdf");
            var result2 = await _service.UploadAsync(content2, "file2.pdf", "application/pdf");

            // Assert
            Assert.NotEqual(result1.BlobUrl, result2.BlobUrl);
        }

        [Fact]
        public async Task DownloadAsync_ShouldReturnFile_WhenBlobExists()
        {
            // Arrange
            var content = "Test file content";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var uploadResult = await _service.UploadAsync(stream, "test.pdf", "application/pdf");

            // Act
            var downloadResult = await _service.DownloadAsync(uploadResult.BlobUrl);

            // Assert
            Assert.NotNull(downloadResult);
            Assert.Equal("application/pdf", downloadResult!.ContentType);
            Assert.Equal("test.pdf", downloadResult.FileName);

            using var reader = new StreamReader(downloadResult.Content);
            var downloadedContent = await reader.ReadToEndAsync();
            Assert.Equal(content, downloadedContent);
        }

        [Fact]
        public async Task DownloadAsync_ShouldReturnNull_WhenBlobDoesNotExist()
        {
            // Act
            var result = await _service.DownloadAsync("https://inmemory.blob/nonexistent/file.pdf");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task DownloadAsync_ShouldThrowArgumentException_WhenBlobUrlIsEmpty()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.DownloadAsync(""));
        }

        [Fact]
        public async Task GenerateDownloadSasUriAsync_ShouldReturnSasUri_WhenBlobExists()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
            var uploadResult = await _service.UploadAsync(stream, "test.pdf", "application/pdf");

            // Act
            var sasUri = await _service.GenerateDownloadSasUriAsync(uploadResult.BlobUrl, TimeSpan.FromHours(1));

            // Assert
            Assert.NotNull(sasUri);
            Assert.NotEmpty(sasUri);
            Assert.Contains("se=", sasUri);
            Assert.Contains("sp=r", sasUri);
        }

        [Fact]
        public async Task GenerateDownloadSasUriAsync_ShouldThrowInvalidOperationException_WhenBlobDoesNotExist()
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GenerateDownloadSasUriAsync("https://inmemory.blob/nonexistent/file.pdf", TimeSpan.FromHours(1)));
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnTrue_WhenBlobExists()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
            var uploadResult = await _service.UploadAsync(stream, "test.pdf", "application/pdf");

            // Act
            var deleted = await _service.DeleteAsync(uploadResult.BlobUrl);

            // Assert
            Assert.True(deleted);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenBlobDoesNotExist()
        {
            // Act
            var deleted = await _service.DeleteAsync("https://inmemory.blob/nonexistent/file.pdf");

            // Assert
            Assert.False(deleted);
        }

        [Fact]
        public async Task DeleteAsync_ShouldMakeBlobUnavailable()
        {
            // Arrange
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
            var uploadResult = await _service.UploadAsync(stream, "test.pdf", "application/pdf");

            // Act
            await _service.DeleteAsync(uploadResult.BlobUrl);

            // Assert
            var downloadResult = await _service.DownloadAsync(uploadResult.BlobUrl);
            Assert.Null(downloadResult);
        }

        [Fact]
        public async Task UploadDownloadRoundTrip_ShouldPreserveContent()
        {
            // Arrange
            var originalContent = "Hello, World! This is a test document.";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(originalContent));

            // Act
            var uploadResult = await _service.UploadAsync(stream, "roundtrip.txt", "text/plain");
            var downloadResult = await _service.DownloadAsync(uploadResult.BlobUrl);

            // Assert
            Assert.NotNull(downloadResult);
            using var reader = new StreamReader(downloadResult!.Content);
            var downloadedContent = await reader.ReadToEndAsync();
            Assert.Equal(originalContent, downloadedContent);
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowArgumentException_WhenBlobUrlIsEmpty()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.DeleteAsync(""));
        }
    }
}
