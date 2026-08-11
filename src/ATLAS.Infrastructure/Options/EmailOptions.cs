//----------------------
// Email Options
// Strongly-typed configuration for email delivery, bound from the "Email" section.
// Production uses Azure Communication Services (ACS) with Managed Identity; local
// development uses a deterministic local sink when ACS is not configured.
//----------------------

#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ATLAS.Infrastructure.Options
{
    /// <summary>
    /// Configuration options for email delivery.
    /// Bound from the "Email" configuration section.
    /// </summary>
    public class EmailOptions : IValidatableObject
    {
        /// <summary>
        /// The configuration section name.
        /// </summary>
        public const string SectionName = "Email";

        /// <summary>
        /// Azure Communication Services configuration.
        /// </summary>
        public AcsEmailOptions Acs { get; set; } = new();

        /// <summary>
        /// Validates the configuration. ACS settings are intentionally empty when the
        /// local development email sink is in use, so they are only required when ACS is
        /// actually configured (i.e. when an endpoint or sender address is present).
        /// </summary>
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var hasEndpoint = !string.IsNullOrWhiteSpace(Acs.Endpoint);
            var hasSenderAddress = !string.IsNullOrWhiteSpace(Acs.SenderAddress);

            // When ACS is partially or fully configured, require complete and valid settings.
            if (!hasEndpoint && !hasSenderAddress)
                yield break;

            if (!hasEndpoint)
                yield return new ValidationResult(
                    "Email:Acs:Endpoint is required when ACS email is configured.",
                    new[] { nameof(Acs) });

            if (!hasSenderAddress)
                yield return new ValidationResult(
                    "Email:Acs:SenderAddress is required when ACS email is configured.",
                    new[] { nameof(Acs) });

            if (hasEndpoint && !Uri.TryCreate(Acs.Endpoint, UriKind.Absolute, out _))
                yield return new ValidationResult(
                    "Email:Acs:Endpoint must be a valid absolute URI.",
                    new[] { nameof(Acs) });
        }
    }

    /// <summary>
    /// Azure Communication Services email configuration.
    /// </summary>
    public class AcsEmailOptions
    {
        /// <summary>
        /// The ACS resource endpoint (e.g. https://atlas-comm-<suffix>.communication.azure.com).
        /// Required when ACS email is configured.
        /// </summary>
        public string? Endpoint { get; set; }

        /// <summary>
        /// The verified sender email address (e.g. DoNotReply@atlas.com) registered with the
        /// ACS email domain. Required when ACS email is configured.
        /// </summary>
        public string? SenderAddress { get; set; }
    }
}
