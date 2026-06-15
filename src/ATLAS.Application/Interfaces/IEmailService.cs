//----------------------
// Email Service Interface
// Defines contract for sending emails
//----------------------

#nullable enable

using System.Threading.Tasks;

namespace ATLAS.Application.Interfaces
{
    public interface IEmailService
    {
        /// <summary>
        /// Send an email asynchronously
        /// </summary>
        Task SendAsync(string to, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default);
    }
}
