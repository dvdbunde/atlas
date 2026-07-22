//----------------------
// Email Template Store Abstraction (Application layer)
// Decouples the renderer and administration from the physical template source.
// The initial implementation is file-backed; future implementations (DB, Azure
// storage) can be added without changing the renderer or the admin UI.
//----------------------

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Application.EmailTemplates
{
    /// <summary>
    /// A single, application-owned email template. Only its <see cref="Content"/> is
    /// mutable; the set of templates is fixed by the application.
    /// </summary>
    public sealed class EmailTemplate
    {
        /// <summary>The fixed, application-owned template name (e.g. "ApprovalNotification").</summary>
        public string Name { get; init; } = string.Empty;

        /// <summary>The current template body using {{Placeholder}} syntax.</summary>
        public string Content { get; init; } = string.Empty;
    }

    /// <summary>
    /// Abstraction over the persistent store of email templates. The renderer depends
    /// on this interface rather than reading files directly, so the source (file,
    /// database, Azure storage) is an implementation detail.
    /// </summary>
    public interface IEmailTemplateStore
    {
        /// <summary>Returns the names of all application-owned templates.</summary>
        Task<IReadOnlyList<string>> GetTemplateNamesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns the template with the given name, or <c>null</c> if it does not exist.
        /// </summary>
        Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Persists the content of an existing template. Implementations must reject
        /// unknown names and confine writes to the managed template set.
        /// </summary>
        Task SaveAsync(EmailTemplate template, CancellationToken cancellationToken = default);
    }
}
