//----------------------
// Email Template Renderer Interface
// Renders email templates with variable substitution
//----------------------

#nullable enable

using System.Threading.Tasks;

namespace ATLAS.Application.Interfaces
{
    public interface IEmailTemplateRenderer
    {
        /// <summary>
        /// Render a template with the provided model
        /// </summary>
        Task<string> RenderAsync(string templateName, object model, CancellationToken cancellationToken = default);
    }
}
