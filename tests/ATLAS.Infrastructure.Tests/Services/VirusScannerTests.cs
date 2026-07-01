using System.IO;
using System.Text;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Infrastructure.Services;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Interfaces
{
    public class VirusScannerTests
    {
        [Fact]
        public async Task PassThroughScanner_ShouldAlwaysReturnClean()
        {
            // Arrange
            var scanner = new PassThroughVirusScanner();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("clean content"));

            // Act
            var result = await scanner.ScanAsync(stream, "test.pdf");

            // Assert
            Assert.True(result.IsClean);
            Assert.Null(result.ThreatName);
        }

        [Fact]
        public async Task PassThroughScanner_ShouldReturnDetails()
        {
            // Arrange
            var scanner = new PassThroughVirusScanner();
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

            // Act
            var result = await scanner.ScanAsync(stream, "test.pdf");

            // Assert
            Assert.NotNull(result.Details);
            Assert.Contains("not yet configured", result.Details);
        }

        [Fact]
        public void IVirusScanner_ShouldBeInterface()
        {
            Assert.True(typeof(IVirusScanner).IsInterface);
        }

        [Fact]
        public void IVirusScanner_ShouldDefineScanAsyncMethod()
        {
            var method = typeof(IVirusScanner).GetMethod("ScanAsync");
            Assert.NotNull(method);
            Assert.Equal(typeof(Task<VirusScanResult>), method!.ReturnType);
        }
    }
}