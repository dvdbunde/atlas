//----------------------
// File-backed Email Template Store (Infrastructure)
// Reads/writes the existing .txt template files. Confines all access to a managed
// set of known template names and a single root directory to prevent path traversal.
//----------------------

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.EmailTemplates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ATLAS.Infrastructure.EmailTemplates
{
    public class FileEmailTemplateStore : IEmailTemplateStore
    {
        // The application owns exactly these four templates. Names are fixed; only
        // content is editable. This allow-list is the primary security boundary.
        private static readonly IReadOnlyList<string> KnownTemplateNames = new List<string>
        {
            "SubmissionConfirmation",
            "ApprovalNotification",
            "RejectionNotification",
            "InfoRequestNotification"
        };

        private readonly string _templatePath;

        public FileEmailTemplateStore(IConfiguration configuration, ILogger<FileEmailTemplateStore> logger)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));

            _templatePath = configuration.GetValue<string>("Email:Templates:Path")
                ?? Path.Combine(AppContext.BaseDirectory, "Templates", "Emails");

            if (!Directory.Exists(_templatePath))
            {
                logger.LogWarning(
                    "Email template directory '{TemplatePath}' does not exist; templates will not be found.",
                    _templatePath);
            }
        }

        public Task<IReadOnlyList<string>> GetTemplateNamesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(KnownTemplateNames);
        }

        public async Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (!IsKnownTemplate(name))
                return null;

            var file = ResolveFile(name);
            if (!File.Exists(file))
                return null;

            var content = await File.ReadAllTextAsync(file, cancellationToken);
            return new EmailTemplate { Name = name, Content = content };
        }

        public async Task SaveAsync(EmailTemplate template, CancellationToken cancellationToken = default)
        {
            if (template is null)
                throw new ArgumentNullException(nameof(template));
            if (!IsKnownTemplate(template.Name))
                throw new ArgumentException($"Unknown email template '{template.Name}'.", nameof(template));

            var file = ResolveFile(template.Name);
            await File.WriteAllTextAsync(file, template.Content ?? string.Empty, cancellationToken);
        }

        private bool IsKnownTemplate(string name)
        {
            return !string.IsNullOrWhiteSpace(name)
                && KnownTemplateNames.Contains(name, StringComparer.OrdinalIgnoreCase);
        }

        private string ResolveFile(string name)
        {
            // name is guaranteed to be in the known allow-list (no path separators),
            // but we still resolve against the root and assert containment to defend
            // against any future refactor that relaxes the allow-list check.
            var file = Path.Combine(_templatePath, $"{name}.txt");
            var fullRoot = Path.GetFullPath(_templatePath);
            var fullFile = Path.GetFullPath(file);

            if (!fullFile.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fullFile, fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Resolved template path escapes the managed template directory.");
            }

            return file;
        }
    }
}
