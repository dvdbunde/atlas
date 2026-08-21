//----------------------
// Azure Communication Services Email Service (Infrastructure)
// Production implementation of IEmailService that delivers rendered emails through
// Azure Communication Services using Managed Identity. Replaces the old SMTP path as
// the production email implementation.
//----------------------

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ATLAS.Infrastructure.Services
{
    public class AcsEmailService : IEmailService
    {
        private readonly IEmailClient _emailClient;
        private readonly string _senderAddress;
        private readonly ILogger<AcsEmailService> _logger;

        public AcsEmailService(
            IEmailClient emailClient,
            IOptions<EmailOptions> emailOptions,
            ILogger<AcsEmailService> logger)
        {
            _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var options = emailOptions?.Value ?? throw new ArgumentNullException(nameof(emailOptions));
            _senderAddress = string.IsNullOrWhiteSpace(options.Acs.SenderAddress)
                ? throw new InvalidOperationException("Email:Acs:SenderAddress must be configured for ACS email delivery.")
                : options.Acs.SenderAddress;
        }

        public async Task SendAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
        {
            // No try/catch here: AcsEmailClient (the dependency boundary) logs ACS
            // failures with service-specific context, and callers (email handlers)
            // log failures with application context. Logging here as well would
            // duplicate the same exception at multiple layers.
            await _emailClient.SendAsync(
                _senderAddress,
                to,
                subject,
                body,
                isHtml,
                cancellationToken);

            _logger.LogInformation("Email sent successfully to {EmailRecipient}, subject: {Subject}", to, subject);
        }
    }
}
