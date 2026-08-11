//----------------------
// Email Template Blob Client Abstraction (Infrastructure)
// A narrow seam over Azure Blob Storage for customized email templates. It keeps the
// store testable without a live Azure environment (the production adapter uses
// BlobServiceClient; tests substitute an in-memory double). Only one customized copy
// per template is supported; there is deliberately no versioning at this stage.
//----------------------

#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Infrastructure.EmailTemplates
{
    /// <summary>
    /// Minimal blob operations used by <see cref="BlobEmailTemplateStore"/> for the
    /// customized-email-template store. Implementations must map a logical blob name
    /// (e.g. "email-templates/ApprovalNotification.txt") to a single deterministic blob.
    /// </summary>
    public interface IEmailTemplateBlobClient
    {
        /// <summary>Returns true when a blob with the given name exists.</summary>
        Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default);

        /// <summary>Downloads and returns the blob content, or null when absent.</summary>
        Task<string?> DownloadAsync(string blobName, CancellationToken cancellationToken = default);

        /// <summary>Uploads content, replacing any existing blob with the same name.</summary>
        Task UploadAsync(string blobName, string content, CancellationToken cancellationToken = default);

        /// <summary>Deletes the blob if present. Idempotent; never throws when absent.</summary>
        Task DeleteAsync(string blobName, CancellationToken cancellationToken = default);
    }
}
