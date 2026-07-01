using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Application.Interfaces
{
    /// <summary>
    /// Abstraction for file virus scanning.
    /// Default implementation is a pass-through no-op.
    /// Replace with real antivirus integration (e.g., Microsoft Defender for Storage) post-MVP.
    /// </summary>
    public interface IVirusScanner
    {
        /// <summary>
        /// Scan a file stream for malware.
        /// </summary>
        /// <param name="fileStream">The file content to scan.</param>
        /// <param name="fileName">The original file name (for extension-based rules).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Scan result indicating whether the file is clean.</returns>
        Task<VirusScanResult> ScanAsync(Stream fileStream, string fileName, CancellationToken ct = default);
    }

    /// <summary>
    /// Result of a virus scan operation.
    /// </summary>
    public class VirusScanResult
    {
        public bool IsClean { get; init; } = true;
        public string? ThreatName { get; init; }
        public string? Details { get; init; }
    }
}