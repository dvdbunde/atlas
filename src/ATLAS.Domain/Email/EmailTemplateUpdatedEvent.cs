//----------------------
// Email Template Updated Domain Event
// Published after a successful template content update so the existing
// event-driven audit infrastructure records the change. The handler (not this
// event) owns audit creation; the command handler stays orchestration-only.
//----------------------

#nullable enable

using System;
using System.Security.Cryptography;
using System.Text;
using MediatR;

namespace ATLAS.Domain.Email
{
    /// <summary>
    /// Raised when an administrator successfully saves an email template. Carries the
    /// template name and the acting administrator's id so the audit handler can record
    /// who changed what, without exposing template content.
    /// </summary>
    public sealed record EmailTemplateUpdatedEvent(string TemplateName, Guid PerformedByUserId) : INotification
    {
        /// <summary>
        /// A stable, deterministic Guid derived from the template name. AuditLog.EntityId
        /// is a Guid, but templates are keyed by name; this lets the audit entry be
        /// queried by a reproducible id without storing content.
        /// </summary>
        public Guid EntityId => EmailTemplateAuditId.For(TemplateName);
    }

    /// <summary>
    /// Produces a deterministic Guid for a template name (name-based via MD5). Stable
    /// across process runs so repeated edits to the same template share one audit id.
    /// </summary>
    internal static class EmailTemplateAuditId
    {
        public static Guid For(string templateName)
        {
            var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"EmailTemplate:{templateName}"));
            return new Guid(bytes);
        }
    }
}
