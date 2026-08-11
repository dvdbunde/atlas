//----------------------
// Local Email Service (Infrastructure)
// Development-only implementation of IEmailService that captures emails deterministically
// by logging them. It does NOT attempt real delivery (no SMTP, no ACS). Used when ACS is
// not configured so developers can exercise the email flow without production secrets.
//----------------------

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace ATLAS.Infrastructure.Services
{
    public class LocalEmailService : IEmailService
    {
        private readonly ILogger<LocalEmailService> _logger;

        public LocalEmailService(ILogger<LocalEmailService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task SendAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
        {
            // Deterministic local capture: log the email so it can be inspected during
            // development. No real delivery is attempted.
            _logger.LogInformation(
                "[LOCAL EMAIL] To: {To} | Subject: {Subject} | Body: {Body}",
                to, subject, body);

            return Task.CompletedTask;
        }
    }
}
