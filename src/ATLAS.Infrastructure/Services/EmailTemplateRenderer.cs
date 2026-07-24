//----------------------
// Email Template Renderer Implementation
// Renders plain text templates with simple variable substitution.
// The template source is abstracted behind IEmailTemplateStore; the renderer no
// longer reads files directly, so the source (file, DB, Azure storage) is swappable.
//----------------------

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.EmailTemplates;
using ATLAS.Application.Interfaces;

namespace ATLAS.Infrastructure.Services
{
    public class EmailTemplateRenderer : IEmailTemplateRenderer
    {
        private readonly IEmailTemplateStore _store;

        public EmailTemplateRenderer(IEmailTemplateStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<string> RenderAsync(string templateName, object model, CancellationToken cancellationToken = default)
        {
            var template = await _store.GetByNameAsync(templateName, cancellationToken);
            if (template is null)
                throw new InvalidOperationException($"Email template not found: {templateName}");

            return ReplaceVariables(template.Content, model);
        }

        /// <summary>
        /// Renders an explicit template body (used by the admin preview feature) without
        /// touching the persistent store. Reuses the same substitution logic.
        /// </summary>
        public Task<string> RenderContentAsync(string content, object model, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ReplaceVariables(content, model));
        }

        private static string ReplaceVariables(string template, object model)
        {
            if (model == null)
            {
                return template;
            }

            var result = template;
            var properties = model.GetType().GetProperties();

            foreach (var prop in properties)
            {
                var value = prop.GetValue(model)?.ToString() ?? string.Empty;
                var placeholder = $"{{{{{prop.Name}}}}}";
                result = result.Replace(placeholder, value);
            }

            return result;
        }
    }
}
