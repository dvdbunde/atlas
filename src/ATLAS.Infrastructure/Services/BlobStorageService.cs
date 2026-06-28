//----------------------
// Blob Storage Service
// Production implementation of IFileStorageService backed by Azure Blob Storage
//----------------------

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Infrastructure.Options;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace ATLAS.Infrastructure.Services
{
    /// <summary>
    /// Production implementation of IFileStorageService using Azure Blob Storage.
    /// Blob naming convention per ADR-015: {applicationId}/{documentId}/{fileName}
    /// </summary>    
    public class BlobStorageService : IFileStorageService
    {
        private readonly BlobContainerClient _containerClient;
        private readonly TimeSpan _sasTokenExpiry;

        public BlobStorageService(IOptions<StorageOptions> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var storageOptions = options.Value;

            if (string.IsNullOrWhiteSpace(storageOptions.ConnectionString))
                throw new InvalidOperationException("Storage:ConnectionString must be configured.");

            if (string.IsNullOrWhiteSpace(storageOptions.ContainerName))
                throw new InvalidOperationException("Storage:ContainerName must be configured.");

            var expiryHours = storageOptions.SasTokenExpiryHours > 0
                ? storageOptions.SasTokenExpiryHours
                : 1;
            _sasTokenExpiry = TimeSpan.FromHours(expiryHours);

            var blobServiceClient = new BlobServiceClient(storageOptions.ConnectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(storageOptions.ContainerName);
        }

               /// <summary>
        /// Upload a file stream to blob storage.
        /// blobPath format per ADR-015: {applicationId}/{documentId}/{fileName}
        /// </summary>
        public async Task<FileUploadResult> UploadAsync(Stream fileStream, string blobPath, string contentType, CancellationToken ct = default)
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream));
                
            if (string.IsNullOrWhiteSpace(blobPath))
                throw new ArgumentException("Blob path must be provided.", nameof(blobPath));

            long fileSize = fileStream.CanSeek ? fileStream.Length : 0;

            await _containerClient.CreateIfNotExistsAsync(cancellationToken: ct);

            // Use caller-provided blob path directly (ADR-015 naming applied by handler)
            var blobClient = _containerClient.GetBlobClient(blobPath);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
            };

            await blobClient.UploadAsync(fileStream, blobHttpHeaders, cancellationToken: ct);

            return new FileUploadResult(blobClient.Uri.ToString(), fileSize);
        }

        /// <summary>
        /// Download a file from blob storage by its blob URL.
        /// Returns null if the blob does not exist.
        /// </summary>
        public async Task<FileDownloadResult?> DownloadAsync(string blobUrl, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
                throw new ArgumentException("Blob URL must be provided.", nameof(blobUrl));

            var blobClient = GetBlobClientFromUrl(blobUrl);

            if (!await blobClient.ExistsAsync(ct))
                return null;

            var properties = await blobClient.GetPropertiesAsync(cancellationToken: ct);
            var contentType = properties.Value.ContentType;
            var fileName = GetFileNameFromBlobUrl(blobUrl);

            var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream, ct);
            memoryStream.Position = 0;

            return new FileDownloadResult(memoryStream, contentType, fileName);
        }

        /// <summary>
        /// Generate a time-limited SAS URI for secure download access.
        /// </summary>
        public Task<string> GenerateDownloadSasUriAsync(string blobUrl, TimeSpan expiry, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
                throw new ArgumentException("Blob URL must be provided.", nameof(blobUrl));

            var blobClient = GetBlobClientFromUrl(blobUrl);

            // Fix #2: Remove TimeSpan.Zero magic value — use stored default instead
            // Fix #3: Add StartsOn with 5-minute clock skew buffer
            var actualExpiry = expiry == TimeSpan.Zero ? _sasTokenExpiry : expiry;
            var startsOn = DateTimeOffset.UtcNow.AddMinutes(-5);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = blobClient.Name,
                Resource = "b",
                StartsOn = startsOn,
                ExpiresOn = startsOn.Add(actualExpiry)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return Task.FromResult(sasUri.ToString());
        }

        /// <summary>
        /// Delete a file from blob storage by its blob URL.
        /// Returns true if the blob was deleted; false if it did not exist.
        /// </summary>
        public async Task<bool> DeleteAsync(string blobUrl, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
                throw new ArgumentException("Blob URL must be provided.", nameof(blobUrl));

            var blobClient = GetBlobClientFromUrl(blobUrl);

            if (!await blobClient.ExistsAsync(ct))
                return false;

            await blobClient.DeleteIfExistsAsync(cancellationToken: ct);
            return true;
        }

        /// <summary>
        /// Extract a BlobClient from a full blob URL by parsing the blob name relative to the container.
        /// Fix #4: Trim leading '/' from AbsolutePath to avoid SDK path resolution issues.
        /// </summary>
        private BlobClient GetBlobClientFromUrl(string blobUrl)
        {
            var blobUri = new Uri(blobUrl);

            // Azure SDK GetBlobClient expects the relative blob path without leading slash.
            // AbsolutePath returns "/containerName/path/to/blob" — trim leading '/' and container prefix.
            var absolutePath = blobUri.AbsolutePath.TrimStart('/');
            // Remove the container name prefix to get the blob name
            var containerPrefix = _containerClient.Name + "/";
            var blobName = absolutePath.StartsWith(containerPrefix, StringComparison.OrdinalIgnoreCase)
                ? absolutePath[containerPrefix.Length..]
                : absolutePath;

            return _containerClient.GetBlobClient(blobName);
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
    }
}