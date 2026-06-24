//----------------------
// File Storage Service Interface
// Defines contract for document blob storage operations
//----------------------

#nullable enable

using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Application.Interfaces
{
    /// <summary>
    /// Provides file storage operations for document management.
    /// All blob access uses BlobUrl as the single storage reference.
    /// </summary>
    public interface IFileStorageService
    {        
        /// <summary>
        /// Upload a file stream to blob storage.
        /// </summary>
        /// <param name="fileStream">The file content stream.</param>
        /// <param name="blobPath">The full blob path in format: {applicationId}/{documentId}/{fileName}.</param>
        /// <param name="contentType">The MIME content type.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A FileUploadResult with the blob URL and size.</returns>
        Task<FileUploadResult> UploadAsync(Stream fileStream, string blobPath, string contentType, CancellationToken ct = default);
        /// <summary>
        /// Download a file from blob storage by its blob URL.
        /// </summary>
        /// <param name="blobUrl">The full blob URL to download.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A FileDownloadResult if found, or null if the blob does not exist.</returns>
        Task<FileDownloadResult?> DownloadAsync(string blobUrl, CancellationToken ct = default);

        /// <summary>
        /// Generate a time-limited SAS URI for secure download access.
        /// </summary>
        /// <param name="blobUrl">The full blob URL.</param>
        /// <param name="expiry">The SAS token validity duration.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A SAS URI string with the specified expiry.</returns>
        Task<string> GenerateDownloadSasUriAsync(string blobUrl, TimeSpan expiry, CancellationToken ct = default);

        /// <summary>
        /// Delete a file from blob storage by its blob URL.
        /// </summary>
        /// <param name="blobUrl">The full blob URL to delete.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True if the blob was deleted; false if it did not exist.</returns>
        Task<bool> DeleteAsync(string blobUrl, CancellationToken ct = default);
    }

    /// <summary>
    /// Result of a successful file upload operation.
    /// </summary>
    /// <param name="BlobUrl">The full URL to the uploaded blob.</param>
    /// <param name="Size">The size of the uploaded file in bytes.</param>
    public record FileUploadResult(string BlobUrl, long Size);

    /// <summary>
    /// Result of a successful file download operation.
    /// </summary>
    /// <param name="Content">The file content stream.</param>
    /// <param name="ContentType">The MIME content type.</param>
    /// <param name="FileName">The original file name.</param>
    public record FileDownloadResult(Stream Content, string ContentType, string FileName);
}