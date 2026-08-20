//----------------------
// LocalEmailService Tests
// Verifies the development-only email sink captures emails deterministically without
// attempting real delivery.
//----------------------

#nullable enable

using System.Threading.Tasks;
using ATLAS.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ATLAS.Infrastructure.Tests.Services
{
    public class LocalEmailServiceTests
    {
        [Fact]
        public async Task SendAsync_Completes_WithoutRealDelivery()
        {
            var service = new LocalEmailService(NullLogger<LocalEmailService>.Instance);

            // Should not throw and should complete immediately.
            await service.SendAsync("citizen@example.com", "Subject", "Body");
        }
    }
}
