//----------------------
// Known Email Template Names
// Single source of truth for the application-owned email template set. The set is
// fixed by the application; only template content is mutable. Shared by the
// Application layer (validation) and Infrastructure layer (store implementations)
// so the allow-list is not duplicated.
//----------------------

#nullable enable

using System;
using System.Collections.Generic;

namespace ATLAS.Application.EmailTemplates
{
    /// <summary>
    /// The fixed set of application-owned email templates. Names are immutable; only
    /// content may be edited. This allow-list is the primary security boundary for
    /// template read/write/reset operations.
    /// </summary>
   public static class KnownEmailTemplates
    {
        public const string SubmissionConfirmation = nameof(SubmissionConfirmation);

        public const string ReSubmissionConfirmation = nameof(ReSubmissionConfirmation);

        public const string ApprovalNotification = nameof(ApprovalNotification);

        public const string RejectionNotification = nameof(RejectionNotification);

        public const string InfoRequestNotification = nameof(InfoRequestNotification);

        public static readonly IReadOnlyList<string> Names =
        [
            SubmissionConfirmation,
            ReSubmissionConfirmation,
            ApprovalNotification,
            RejectionNotification,
            InfoRequestNotification
        ];
    }
}
