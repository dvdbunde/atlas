//----------------------
// In-Memory File Storage Service
// Test double implementation of IFileStorageService with no Azure dependency
//----------------------

#nullable enable

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;

namespace ATLAS.Infrastructure.Services
{
    /// <summary>
    /// In-memory implementation of IFileStorageService for unit and integration tests.
    /// Uses a ConcurrentDictionary to store blob content keyed by blob URL.
    /// No Azure or external dependencies required.
    /// </summary>
    public class InMemoryFileStorageService : IFileStorageService
    {
        private readonly ConcurrentDictionary<string, InMemoryBlob> _blobs = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Upload a file stream to in-memory storage.
        /// Generates a blob URL with the format: https://inmemory.blob/{guid}/{guid}/{fileName}
        /// </summary>
        public Task<FileUploadResult> UploadAsync(Stream fileStream, string blobPath, string contentType, CancellationToken ct = default)
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));

            if (string.IsNullOrWhiteSpace(blobPath))
                throw new ArgumentException("Blob path must be provided.", nameof(blobPath));

            var memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);
            memoryStream.Position = 0;

            var blobUrl = $"https://inmemory.blob/{blobPath}";

            var blob = new InMemoryBlob(memoryStream, contentType);
            _blobs[blobUrl] = blob;

            return Task.FromResult(new FileUploadResult(blobUrl, memoryStream.Length));
        }

        /// <summary>
        /// Download a file from in-memory storage by its blob URL.
        /// Returns null if the blob does not exist.
        /// </summary>
        public Task<FileDownloadResult?> DownloadAsync(string blobUrl, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
                throw new ArgumentException("Blob URL must be provided.", nameof(blobUrl));

            if (!_blobs.TryGetValue(blobUrl, out var blob))
                return Task.FromResult<FileDownloadResult?>(null);

            var fileName = GetFileNameFromBlobUrl(blobUrl);
            var contentStream = new MemoryStream(blob.Data);
            contentStream.Position = 0;

            return Task.FromResult<FileDownloadResult?>(new FileDownloadResult(contentStream, blob.ContentType, fileName));
        }

        /// <summary>
        /// Generate a simulated SAS URI (returns the blob URL with sas token placeholder).
        /// In-memory implementation does not enforce token expiry.
        /// </summary>
        public Task<string> GenerateDownloadSasUriAsync(string blobUrl, TimeSpan expiry, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
                throw new ArgumentException("Blob URL must be provided.", nameof(blobUrl));

            if (!_blobs.ContainsKey(blobUrl))
                throw new InvalidOperationException($"Blob not found: {blobUrl}");

            // Return a simulated SAS URI for compatibility
            var sasUri = $"{blobUrl}?sv=2020-08-04&se={DateTimeOffset.UtcNow.Add(expiry):yyyy-MM-ddTHH:mm:ssZ}&sr=b&sp=r";
            return Task.FromResult(sasUri);
        }

        /// <summary>
        /// Delete a file from in-memory storage by its blob URL.
        /// Returns true if the blob was deleted; false if it did not exist.
        /// </summary>
        public Task<bool> DeleteAsync(string blobUrl, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
                throw new ArgumentException("Blob URL must be provided.", nameof(blobUrl));

            var removed = _blobs.TryRemove(blobUrl, out _);
            return Task.FromResult(removed);
        }

        /// <summary>
        /// Extract the file name from the blob URL (last path segment).
        /// </summary>
        private static string GetFileNameFromBlobUrl(string blobUrl)
        {
            var uri = new Uri(blobUrl);
            var segments = uri.Segments;
            return segments.Length > 0 ? segments[^1].Trim('/') : "unknown";
        }

        /// <summary>
        /// Internal blob storage record.
        /// </summary>
        private sealed record InMemoryBlob(byte[] Data, string ContentType)
        {
            public InMemoryBlob(Stream stream, string contentType)
                : this(ReadAllBytes(stream), contentType)
            {
            }

            private static byte[] ReadAllBytes(Stream stream)
            {
                using var memoryStream = new MemoryStream();
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
