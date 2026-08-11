//----------------------
// Blob-backed Email Template Store (Infrastructure)
// Composes the immutable source-code default templates (read directly from
// AppContext.BaseDirectory/Templates/Emails) with Azure Blob Storage for customized
// templates. Resolution: prefer a customized blob when one exists, otherwise fall
// back to the source-code default. Save writes to Blob (never the source). Reset
// deletes the customized blob (never writes the default back).
//
// When Blob Storage is not configured (local development), the injected
// IEmailTemplateBlobClient is null and the store serves the source-code defaults
// only, so local development needs no Azure dependency.
//----------------------

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.EmailTemplates;
using Microsoft.Extensions.Logging;

namespace ATLAS.Infrastructure.EmailTemplates
{
    public class BlobEmailTemplateStore : IEmailTemplateStore
    {
        // Deterministic blob name for a template's customized version. No versioning:
        // at most one customized copy per template.
        internal const string BlobFolder = "email-templates";

        private readonly IEmailTemplateBlobClient? _blobClient;
        private readonly string _defaultTemplatePath;
        private readonly ILogger<BlobEmailTemplateStore> _logger;

        public BlobEmailTemplateStore(
            IEmailTemplateBlobClient? blobClient,
            ILogger<BlobEmailTemplateStore> logger)
            : this(blobClient, Path.Combine(AppContext.BaseDirectory, "Templates", "Emails"), logger)
        {
        }

        /// <summary>Internal constructor for tests to supply a custom default template path.</summary>
        internal BlobEmailTemplateStore(
            IEmailTemplateBlobClient? blobClient,
            string defaultTemplatePath,
            ILogger<BlobEmailTemplateStore> logger)
        {
            _blobClient = blobClient;
            _defaultTemplatePath = defaultTemplatePath;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (blobClient is null)
            {
                _logger.LogInformation(
                    "Blob Storage is not configured for email templates; using source-code defaults only.");
            }
        }

        public Task<IReadOnlyList<string>> GetTemplateNamesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(KnownEmailTemplates.Names);
        }

        public async Task<EmailTemplate?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            if (!KnownEmailTemplates.IsKnown(name))
                return null;

            if (_blobClient is not null)
            {
                var blobName = BlobName(name);

                // A missing customized blob is NOT an error; it means "use the default".
                if (await _blobClient.ExistsAsync(blobName, cancellationToken))
                {
                    var content = await DownloadContentAsync(blobName, name, cancellationToken);
                    if (content is not null)
                        return new EmailTemplate { Name = name, Content = content };
                }
            }

            // Fall back to the immutable source-code default.
            return await ReadDefaultAsync(name, cancellationToken);
        }

        public async Task SaveAsync(EmailTemplate template, CancellationToken cancellationToken = default)
        {
            if (template is null)
                throw new ArgumentNullException(nameof(template));
            if (!KnownEmailTemplates.IsKnown(template.Name))
                throw new ArgumentException($"Unknown email template '{template.Name}'.", nameof(template));

            if (_blobClient is null)
            {
                // No Blob Storage configured (local dev). There is no durable custom
                // store, so saving can only fall back to the file default; log it.
                _logger.LogWarning(
                    "Save of email template '{TemplateName}' was ignored because Blob Storage is not configured.",
                    template.Name);
                return;
            }

            await _blobClient.UploadAsync(BlobName(template.Name), template.Content ?? string.Empty, cancellationToken);
        }

        public async Task ResetAsync(string name, CancellationToken cancellationToken = default)
        {
            if (!KnownEmailTemplates.IsKnown(name))
                throw new ArgumentException($"Unknown email template '{name}'.", nameof(name));

            if (_blobClient is null)
            {
                // No Blob Storage configured; nothing to delete. The default remains.
                return;
            }

            await _blobClient.DeleteAsync(BlobName(name), cancellationToken);
        }

        internal static string BlobName(string name) => $"{BlobFolder}/{name}.txt";

        private async Task<EmailTemplate?> ReadDefaultAsync(string name, CancellationToken cancellationToken)
        {
            var file = ResolveDefaultFile(name);
            if (!File.Exists(file))
                return null;

            var content = await File.ReadAllTextAsync(file, cancellationToken);
            return new EmailTemplate { Name = name, Content = content };
        }

        private string ResolveDefaultFile(string name)
        {
            var file = Path.Combine(_defaultTemplatePath, $"{name}.txt");
            var fullRoot = Path.GetFullPath(_defaultTemplatePath);
            var fullFile = Path.GetFullPath(file);

            if (!fullFile.StartsWith(fullRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(fullFile, fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Resolved default template path escapes the managed template directory.");
            }

            return file;
        }

        private async Task<string?> DownloadContentAsync(
            string blobName, string templateName, CancellationToken cancellationToken)
        {
            try
            {
                return await _blobClient!.DownloadAsync(blobName, cancellationToken);
            }
            catch (Azure.RequestFailedException ex)
            {
                // A genuine storage failure (auth, connectivity, permission) must not be
                // silently treated as "no customization exists". Surface it so the
                // problem is visible rather than hiding infrastructure issues.
                _logger.LogError(ex, "Failed to read customized email template '{TemplateName}' from Blob Storage.", templateName);
                throw;
            }
        }
    }
}
