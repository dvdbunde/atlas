//----------------------
// Azure Communication Services Email Client (Infrastructure)
// Production implementation of IEmailClient backed by the ACS Email SDK. Authenticates
// using DefaultAzureCredential (Managed Identity in Azure) so no connection string or
// access key is required in production.
//----------------------

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Communication.Email;
using Azure.Identity;
using Microsoft.Extensions.Logging;

namespace ATLAS.Infrastructure.Services
{
    public class AcsEmailClient : IEmailClient
    {
        private readonly EmailClient _emailClient;
        private readonly ILogger<AcsEmailClient> _logger;

        public AcsEmailClient(Uri endpoint, ILogger<AcsEmailClient> logger)
        {
            _emailClient = new EmailClient(endpoint, new DefaultAzureCredential());
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Internal constructor for tests to inject a mocked EmailClient.</summary>
        internal AcsEmailClient(EmailClient emailClient, ILogger<AcsEmailClient> logger)
        {
            _emailClient = emailClient ?? throw new ArgumentNullException(nameof(emailClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendAsync(
            string senderAddress,
            string recipientAddress,
            string subject,
            string content,
            bool isHtml = false,
            CancellationToken cancellationToken = default)
        {
            var emailContent = new EmailContent(subject);

            // Honor the IEmailService contract: HTML content goes in Html, otherwise PlainText.
            if (isHtml)
            {
                emailContent.Html = content;
            }
            else
            {
                emailContent.PlainText = content;
            }

            var message = new EmailMessage(
                senderAddress,
                recipientAddress,
                emailContent);

            var operation = await _emailClient.SendAsync(
                WaitUntil.Completed,
                message,
                cancellationToken);

            _logger.LogInformation(
                "ACS email send completed with status {Status} for recipient {Recipient}",
                operation.Value.Status, recipientAddress);
        }
    }
}
