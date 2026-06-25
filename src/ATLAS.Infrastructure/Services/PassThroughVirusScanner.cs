using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;

namespace ATLAS.Infrastructure.Services
{
    /// <summary>
    /// Pass-through virus scanner implementation for MVP.
    /// Always reports files as clean. Replace with real scanner post-MVP.
    /// </summary>
    public class PassThroughVirusScanner : IVirusScanner
    {
        public Task<VirusScanResult> ScanAsync(Stream fileStream, string fileName, CancellationToken ct = default)
        {
            // MVP: no actual scanning — always pass
            return Task.FromResult(new VirusScanResult
            {
                IsClean = true,
                ThreatName = null,
                Details = "Virus scanning not yet configured."
            });
        }
    }
}