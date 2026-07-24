//----------------------
// Email Template Placeholder Validator (Domain pure utility)
// Extracts {{Placeholder}} tokens and detects unknown ones.
// This is a pure, dependency-free function so it can live in the Domain layer.
//----------------------

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ATLAS.Domain.Email
{
    /// <summary>
    /// Pure helper for working with email-template placeholder tokens of the form
    /// <c>{{PropertyName}}</c>. It extracts tokens and reports any that are not in a
    /// supplied known set, enabling validation that prevents accidental corruption
    /// (e.g. misspelled tokens) without inventing a new template language.
    /// </summary>
    public static class EmailTemplatePlaceholderValidator
    {
        // Matches {{ Name }} allowing optional surrounding whitespace.
        private static readonly Regex PlaceholderRegex = new(
            @"\{\{\s*([A-Za-z_][A-Za-z0-9_]*)\s*\}\}",
            RegexOptions.Compiled);

        /// <summary>Extract all distinct placeholder names from the given content.</summary>
        public static IReadOnlyList<string> ExtractPlaceholders(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return new List<string>();

            var result = new List<string>();
            foreach (Match match in PlaceholderRegex.Matches(content!))
            {
                var name = match.Groups[1].Value;
                if (!result.Contains(name))
                    result.Add(name);
            }

            return result;
        }

        /// <summary>
        /// Returns the placeholders in <paramref name="content"/> that are NOT present
        /// in <paramref name="knownPlaceholders"/> (case-insensitive).
        /// </summary>
        public static IReadOnlyList<string> GetUnknownPlaceholders(
            string? content,
            IEnumerable<string> knownPlaceholders)
        {
            var known = new HashSet<string>(knownPlaceholders, StringComparer.OrdinalIgnoreCase);
            return ExtractPlaceholders(content)
                .Where(p => !known.Contains(p))
                .Distinct()
                .ToList();
        }
    }
}
