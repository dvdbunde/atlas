//----------------------
// Email Client Abstraction (Infrastructure)
// A narrow seam over the Azure Communication Services Email SDK so the ACS sender is
// testable without a live ACS resource. The production adapter wraps EmailClient;
// tests substitute an in-memory/fake double.
//----------------------

#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Infrastructure.Services
{
    /// <summary>
    /// Minimal email-sending operations used by <see cref="AcsEmailService"/>. Keeps the
    /// ACS SDK details out of the sender's core logic and makes it unit-testable.
    /// </summary>
    public interface IEmailClient
    {
        /// <summary>
        /// Sends an email and waits for the ACS send operation to complete.
        /// </summary>
        Task SendAsync(
            string senderAddress,
            string recipientAddress,
            string subject,
            string content,
            bool isHtml = false,
            CancellationToken cancellationToken = default);
    }
}
