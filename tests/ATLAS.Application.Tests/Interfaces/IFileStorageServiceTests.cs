using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using Xunit;

namespace ATLAS.Application.Tests.Interfaces
{
    public class IFileStorageServiceTests
    {
        [Fact]
        public void FileUploadResult_ShouldStoreBlobUrlAndSize()
        {
            // Arrange
            const string expectedBlobUrl = "https://storage.blob.core.windows.net/permit-documents/doc-id/file.pdf";
            const long expectedSize = 1024L;

            // Act
            var result = new FileUploadResult(expectedBlobUrl, expectedSize);

            // Assert
            Assert.Equal(expectedBlobUrl, result.BlobUrl);
            Assert.Equal(expectedSize, result.Size);
        }

        [Fact]
        public void FileDownloadResult_ShouldStoreContentContentTypeAndFileName()
        {
            // Arrange
            using var stream = new MemoryStream(new byte[] { 0x1, 0x2, 0x3 });
            const string contentType = "application/pdf";
            const string fileName = "document.pdf";

            // Act
            var result = new FileDownloadResult(stream, contentType, fileName);

            // Assert
            Assert.Same(stream, result.Content);
            Assert.Equal(contentType, result.ContentType);
            Assert.Equal(fileName, result.FileName);
        }

        [Fact]
        public void FileUploadResult_ShouldBeRecordType()
        {
            // Arrange
            var result1 = new FileUploadResult("url1", 100);
            var result2 = new FileUploadResult("url1", 100);
            var result3 = new FileUploadResult("url2", 200);

            // Assert — records have value equality
            Assert.Equal(result1, result2);
            Assert.NotEqual(result1, result3);
        }

        [Fact]
        public void FileDownloadResult_ShouldBeRecordType()
        {
            // Arrange
            using var stream1 = new MemoryStream();
            using var stream2 = new MemoryStream();
            var result1 = new FileDownloadResult(stream1, "a", "a.txt");
            var result2 = new FileDownloadResult(stream1, "a", "a.txt");
            var result3 = new FileDownloadResult(stream2, "b", "b.txt");

            // Assert — records have reference equality for stream, value equality for primitives
            Assert.Equal(result1, result2);
            Assert.NotEqual(result1, result3);
        }

        [Fact]
        public void IFileStorageService_ShouldDefineUploadAsyncMethod()
        {
            // This is a contract verification — the interface method signatures
            // are the specification. We verify the delegate matches expectations.
            var method = typeof(IFileStorageService).GetMethod(nameof(IFileStorageService.UploadAsync));
            Assert.NotNull(method);
            Assert.Equal(typeof(Task<FileUploadResult>), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Contains(parameters, p => p.Name == "fileStream" && p.ParameterType == typeof(Stream));
            Assert.Contains(parameters, p => p.Name == "blobPath" && p.ParameterType == typeof(string));
            Assert.Contains(parameters, p => p.Name == "contentType" && p.ParameterType == typeof(string));
            Assert.Contains(parameters, p => p.Name == "ct" && p.ParameterType == typeof(CancellationToken));
        }

        [Fact]
        public void IFileStorageService_ShouldDefineDownloadAsyncMethod()
        {
            // Act
            var method = typeof(IFileStorageService).GetMethod(nameof(IFileStorageService.DownloadAsync));

            // Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(Task<FileDownloadResult?>), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Contains(parameters, p => p.Name == "blobUrl" && p.ParameterType == typeof(string));
            Assert.Contains(parameters, p => p.Name == "ct" && p.ParameterType == typeof(CancellationToken));
        }

        [Fact]
        public void IFileStorageService_ShouldDefineGenerateDownloadSasUriAsyncMethod()
        {
            // Act
            var method = typeof(IFileStorageService).GetMethod(nameof(IFileStorageService.GenerateDownloadSasUriAsync));

            // Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(Task<string>), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Contains(parameters, p => p.Name == "blobUrl" && p.ParameterType == typeof(string));
            Assert.Contains(parameters, p => p.Name == "expiry" && p.ParameterType == typeof(TimeSpan));
            Assert.Contains(parameters, p => p.Name == "ct" && p.ParameterType == typeof(CancellationToken));
        }

        [Fact]
        public void IFileStorageService_ShouldDefineDeleteAsyncMethod()
        {
            // Act
            var method = typeof(IFileStorageService).GetMethod(nameof(IFileStorageService.DeleteAsync));

            // Assert
            Assert.NotNull(method);
            Assert.Equal(typeof(Task<bool>), method!.ReturnType);

            var parameters = method.GetParameters();
            Assert.Contains(parameters, p => p.Name == "blobUrl" && p.ParameterType == typeof(string));
            Assert.Contains(parameters, p => p.Name == "ct" && p.ParameterType == typeof(CancellationToken));
        }

        [Fact]
        public void IFileStorageService_ShouldNotDependOnAspNetCoreAbstractions()
        {
            // Verify no ASP.NET Core or IFormFile dependencies
            var ifAssembly = typeof(IFileStorageService).Assembly;
            var referencedAssemblies = ifAssembly.GetReferencedAssemblies();

            Assert.DoesNotContain(referencedAssemblies, a =>
                a.Name != null && a.Name.Contains("AspNetCore", StringComparison.OrdinalIgnoreCase));
        }
    }
}