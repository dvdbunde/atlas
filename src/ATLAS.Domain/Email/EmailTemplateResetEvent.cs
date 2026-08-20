//----------------------
// Email Template Reset Domain Event
// Published after a successful template reset so the existing event-driven audit
// infrastructure records the change. Mirrors EmailTemplateUpdatedEvent; the handler
// (not this event) owns audit creation.
//----------------------

#nullable enable

using System;
using MediatR;

namespace ATLAS.Domain.Email
{
    /// <summary>
    /// Raised when an administrator successfully resets an email template to its
    /// default. Carries the template name so the audit handler can record what
    /// changed, without exposing template content.
    /// </summary>
    public sealed record EmailTemplateResetEvent(string TemplateName) : INotification
    {
        /// <summary>
        /// A stable, deterministic Guid derived from the template name (same scheme as
        /// <see cref="EmailTemplateUpdatedEvent"/> so audit entries share one id).
        /// </summary>
        public Guid EntityId => EmailTemplateAuditId.For(TemplateName);
    }
}
