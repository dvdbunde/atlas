//----------------------
// SMTP Email Service Implementation
// Sends emails via SMTP (development/default)
//----------------------

#nullable enable

using System;
using System.Net;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ATLAS.Infrastructure.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task SendAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var smtpSettings = _configuration.GetSection("Email:Smtp");
                var host = smtpSettings["Host"] ?? "localhost";
                var port = int.Parse(smtpSettings["Port"] ?? "1025");
                var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "false");
                var username = smtpSettings["Username"];
                var password = smtpSettings["Password"];
                var from = smtpSettings["From"] ?? "noreply@atlas.local";
                var fromName = smtpSettings["FromName"] ?? "ATLAS System";

                using var client = new SmtpClient(host, port)
                {
                    EnableSsl = enableSsl,
                    Credentials = !string.IsNullOrEmpty(username) ? new NetworkCredential(username, password) : null
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(from, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = isHtml
                };
                message.To.Add(to);

                await client.SendMailAsync(message, cancellationToken);
                _logger.LogInformation("Email sent successfully to {To}, subject: {Subject}", to, subject);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {To}, subject: {Subject}", to, subject);
                // Don't re-throw - email failure should not block workflow
            }
        }
    }
}
