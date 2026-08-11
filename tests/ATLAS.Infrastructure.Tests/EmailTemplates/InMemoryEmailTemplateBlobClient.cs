//----------------------
// In-Memory Email Template Blob Client
// Test double implementation of IEmailTemplateBlobClient with no Azure dependency.
// Backed by a ConcurrentDictionary keyed by blob name. Used by unit tests to exercise
// the BlobEmailTemplateStore resolution/save/reset logic without live Azure.
//----------------------

#nullable enable

using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Infrastructure.EmailTemplates
{
    public sealed class InMemoryEmailTemplateBlobClient : IEmailTemplateBlobClient
    {
        private readonly ConcurrentDictionary<string, string> _blobs = new(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>Number of distinct blobs currently stored.</summary>
        public int Count => _blobs.Count;

        public Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_blobs.ContainsKey(blobName));
        }

        public Task<string?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
        {
            _blobs.TryGetValue(blobName, out var content);
            return Task.FromResult<string?>(content);
        }

        public Task UploadAsync(string blobName, string content, CancellationToken cancellationToken = default)
        {
            _blobs[blobName] = content;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
        {
            _blobs.TryRemove(blobName, out _);
            return Task.CompletedTask;
        }
    }
}
