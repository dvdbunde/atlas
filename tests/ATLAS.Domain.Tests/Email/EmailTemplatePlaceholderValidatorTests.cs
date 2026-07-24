using System.Collections.Generic;
using ATLAS.Domain.Email;
using Xunit;

namespace ATLAS.Domain.Tests.Email
{
    public class EmailTemplatePlaceholderValidatorTests
    {
        private static readonly IReadOnlyList<string> Known = new List<string>
        {
            "ApplicationNumber", "PermitTypeName", "Status", "CitizenName", "Message", "ReasonCode"
        };

        [Fact]
        public void GetUnknownPlaceholders_ReturnsEmpty_WhenAllKnown()
        {
            var content = "App {{ApplicationNumber}} for {{CitizenName}}: {{Status}}";
            var unknown = EmailTemplatePlaceholderValidator.GetUnknownPlaceholders(content, Known);
            Assert.Empty(unknown);
        }

        [Fact]
        public void GetUnknownPlaceholders_IsCaseInsensitive()
        {
            var content = "Hi {{citizenname}}";
            var unknown = EmailTemplatePlaceholderValidator.GetUnknownPlaceholders(content, Known);
            Assert.Empty(unknown);
        }

        [Fact]
        public void GetUnknownPlaceholders_ReportsUnknownTokens()
        {
            var content = "Dear {{CitizenName}}, {{Typo}} and {{MISSING}}";
            var unknown = EmailTemplatePlaceholderValidator.GetUnknownPlaceholders(content, Known);
            Assert.Contains("Typo", unknown);
            Assert.Contains("MISSING", unknown);
            Assert.DoesNotContain("CitizenName", unknown);
        }

        [Fact]
        public void GetUnknownPlaceholders_HandlesWhitespaceInTokens()
        {
            var content = "{{ ApplicationNumber }}";
            var unknown = EmailTemplatePlaceholderValidator.GetUnknownPlaceholders(content, Known);
            Assert.Empty(unknown);
        }

        [Fact]
        public void ExtractPlaceholders_ReturnsDistinctNames()
        {
            var content = "{{Status}} {{Status}} {{CitizenName}}";
            var extracted = EmailTemplatePlaceholderValidator.ExtractPlaceholders(content);
            Assert.Equal(2, extracted.Count);
        }

        [Fact]
        public void GetUnknownPlaceholders_HandlesNullContent()
        {
            var unknown = EmailTemplatePlaceholderValidator.GetUnknownPlaceholders(null, Known);
            Assert.Empty(unknown);
        }
    }
}

