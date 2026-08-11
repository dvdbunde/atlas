//----------------------
// Azure Blob Email Template Client (Infrastructure)
// Production implementation of IEmailTemplateBlobClient backed by Azure Blob Storage
// using the storage account configured via Storage:AccountName (Managed Identity) or
// Storage:ConnectionString (local Azurite). Uses BlobServiceClient, the same client
// the rest of ATLAS uses for document storage, so no storage account keys are needed
// in production.
//----------------------

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace ATLAS.Infrastructure.EmailTemplates
{
    public class AzureBlobEmailTemplateClient : IEmailTemplateBlobClient
    {
        private readonly BlobContainerClient _containerClient;
        private readonly ILogger<AzureBlobEmailTemplateClient> _logger;

        public AzureBlobEmailTemplateClient(BlobServiceClient blobServiceClient, string containerName, ILogger<AzureBlobEmailTemplateClient> logger)
        {
            _containerClient = (blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient)))
                .GetBlobContainerClient(containerName);
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default)
        {
            return await _containerClient.GetBlobClient(blobName).ExistsAsync(cancellationToken);
        }

        public async Task<string?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);

            if (!await blobClient.ExistsAsync(cancellationToken))
                return null;

            try
            {
                using var stream = new MemoryStream();
                await blobClient.DownloadToAsync(stream, cancellationToken);
                stream.Position = 0;
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync(cancellationToken);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Failed to download email template blob '{BlobName}'.", blobName);
                throw;
            }
        }

        public async Task UploadAsync(string blobName, string content, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await _containerClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            await blobClient.UploadAsync(new BinaryData(content ?? string.Empty), overwrite: true, cancellationToken: cancellationToken);
        }

        public async Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
        }
    }
}
