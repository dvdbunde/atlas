//----------------------
// Email Template Renderer Implementation
// Renders plain text templates with simple variable substitution
//----------------------

#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ATLAS.Infrastructure.Services
{
    public class EmailTemplateRenderer : IEmailTemplateRenderer
    {
        private readonly string _templatePath;

        public EmailTemplateRenderer(IConfiguration configuration)
        {
            _templatePath = configuration.GetValue<string>("Email:Templates:Path") 
                ?? Path.Combine(AppContext.BaseDirectory, "Templates", "Emails");
        }

        public async Task<string> RenderAsync(string templateName, object model, CancellationToken cancellationToken = default)
        {
            var templateFile = Path.Combine(_templatePath, $"{templateName}.txt");
            
            if (!File.Exists(templateFile))
            {
                throw new FileNotFoundException($"Email template not found: {templateFile}");
            }

            var template = await File.ReadAllTextAsync(templateFile, cancellationToken);
            return ReplaceVariables(template, model);
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
