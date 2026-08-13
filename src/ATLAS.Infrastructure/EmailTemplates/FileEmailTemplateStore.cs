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
        private readonly string _templatePath;

        public FileEmailTemplateStore(IConfiguration configuration, ILogger<FileEmailTemplateStore> logger)
        {
            _ = logger ?? throw new ArgumentNullException(nameof(logger));

            _templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "Emails");

            if (!Directory.Exists(_templatePath))
            {
                logger.LogWarning(
                    "Email template directory '{TemplatePath}' does not exist; templates will not be found.",
                    _templatePath);
            }
        }

        public Task<IReadOnlyList<string>> GetTemplateNamesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(KnownEmailTemplates.Names);
        }

        public async Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (!KnownEmailTemplates.Names.Contains(name))
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
            if (!KnownEmailTemplates.Names.Contains(template.Name))
                throw new ArgumentException($"Unknown email template '{template.Name}'.", nameof(template));

            var file = ResolveFile(template.Name);
            await File.WriteAllTextAsync(file, template.Content ?? string.Empty, cancellationToken);
        }

        public Task ResetAsync(string name, CancellationToken cancellationToken = default)
        {
            if (!KnownEmailTemplates.Names.Contains (name))
                throw new ArgumentException($"Unknown email template '{name}'.", nameof(name));

            // The file store IS the source-code default; there is no customization to
            // delete. Reset is a no-op that leaves the default intact.
            return Task.CompletedTask;
        }

        private bool IsKnownTemplate(string name)
        {
            return KnownEmailTemplates.Names.Contains(name);
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
