//----------------------
// InMemoryFileStorageService Tests
// Tests for the in-memory file storage test double
//----------------------

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ATLAS.Infrastructure.Services;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Services
{
    public class InMemoryFileStorageServiceTests
    {
        private readonly InMemoryFileStorageService _service;

        public InMemoryFileStorageServiceTests()
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
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _service.UploadAsync(null!, "test.pdf", "application/pdf"));
        }

        [Fact]
        public async Task UploadAsync_ShouldThrowArgumentException_WhenFileNameIsEmpty()
        {
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UploadAsync(stream, "", "application/pdf"));
        }

        [Fact]
        public async Task DownloadAsync_ShouldReturnFile_WhenBlobExists()
        {
            // Arrange
            var content = "Test content";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            var uploadResult = await _service.UploadAsync(stream, "test.pdf", "application/pdf");

            // Act
            var downloadResult = await _service.DownloadAsync(uploadResult.BlobUrl);

            // Assert
            Assert.NotNull(downloadResult);
            Assert.Equal("application/pdf", downloadResult!.ContentType);
            Assert.Equal("test.pdf", downloadResult.FileName);

            using var reader = new StreamReader(downloadResult.Content);
            Assert.Equal(content, await reader.ReadToEndAsync());
        }

        [Fact]
        public async Task DownloadAsync_ShouldReturnNull_WhenBlobDoesNotExist()
        {
            var result = await _service.DownloadAsync("https://inmemory.blob/nonexistent/file.pdf");
            Assert.Null(result);
        }

        [Fact]
        public async Task DownloadAsync_ShouldThrowArgumentException_WhenBlobUrlIsEmpty()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.DownloadAsync(""));
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
            var deleted = await _service.DeleteAsync("https://inmemory.blob/nonexistent/file.pdf");
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
            Assert.Null(await _service.DownloadAsync(uploadResult.BlobUrl));
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
            Assert.Equal(originalContent, await reader.ReadToEndAsync());
        }

        [Fact]
        public async Task DeleteAsync_ShouldThrowArgumentException_WhenBlobUrlIsEmpty()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.DeleteAsync(""));
        }

        [Fact]
        public async Task UploadAsync_ShouldGenerateUniqueBlobUrls()
        {
            // Arrange
            var stream1 = new MemoryStream(Encoding.UTF8.GetBytes("File 1"));
            var stream2 = new MemoryStream(Encoding.UTF8.GetBytes("File 2"));

            // Act
            var result1 = await _service.UploadAsync(stream1, "file1.pdf", "application/pdf");
            var result2 = await _service.UploadAsync(stream2, "file2.pdf", "application/pdf");

            // Assert
            Assert.NotEqual(result1.BlobUrl, result2.BlobUrl);
        }

        [Fact]
        public async Task GenerateDownloadSasUriAsync_ShouldReturnSasUri()
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
        public async Task GenerateDownloadSasUriAsync_ShouldThrow_WhenBlobDoesNotExist()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.GenerateDownloadSasUriAsync("https://inmemory.blob/nonexistent/file.pdf", TimeSpan.FromHours(1)));
        }

        [Fact]
        public async Task GenerateDownloadSasUriAsync_ShouldThrow_WhenBlobUrlIsEmpty()
        {
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.GenerateDownloadSasUriAsync("", TimeSpan.FromHours(1)));
        }

        [Fact]
        public async Task MultipleUploads_ShouldAllBeDownloadable()
        {
            // Arrange
            var streams = new[]
            {
                (new MemoryStream(Encoding.UTF8.GetBytes("Doc 1")), "doc1.pdf"),
                (new MemoryStream(Encoding.UTF8.GetBytes("Doc 2")), "doc2.pdf"),
                (new MemoryStream(Encoding.UTF8.GetBytes("Doc 3")), "doc3.pdf")
            };

            // Act
            var results = await Task.WhenAll(
                _service.UploadAsync(streams[0].Item1, streams[0].Item2, "application/pdf"),
                _service.UploadAsync(streams[1].Item1, streams[1].Item2, "application/pdf"),
                _service.UploadAsync(streams[2].Item1, streams[2].Item2, "application/pdf")
            );

            // Assert - all uploaded
            foreach (var result in results)
            {
                var download = await _service.DownloadAsync(result.BlobUrl);
                Assert.NotNull(download);
            }
        }
    }
}
