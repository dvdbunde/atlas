//----------------------
// Known Email Template Placeholders
// Derived from the model used by the email rendering pipeline:
//   - ApplicationSummaryDto (used by Submission/Approval handlers)
//   - Message / ReasonCode (used by InfoRequest / Rejection handlers)
// The editor surfaces these so administrators do not accidentally corrupt tokens.
//----------------------

#nullable enable

using System.Collections.Generic;
using ATLAS.Application.DTOs;

namespace ATLAS.Application.EmailTemplates
{
    public static class KnownEmailPlaceholders
    {
        /// <summary>
        /// Placeholder names that are valid across the email templates. These mirror the
        /// properties the rendering pipeline actually supplies at send time.
        /// </summary>
        public static IReadOnlyList<string> All { get; } = new List<string>
        {
            // ApplicationSummaryDto
            nameof(ApplicationSummaryDto.ApplicationNumber),
            nameof(ApplicationSummaryDto.PermitTypeName),
            nameof(ApplicationSummaryDto.Status),
            nameof(ApplicationSummaryDto.CitizenName),
            // InfoRequestNotification
            "Message",
            // RejectionNotification
            "ReasonCode"
        };
    }
}
