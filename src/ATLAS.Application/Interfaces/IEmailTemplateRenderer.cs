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

        /// <summary>
        /// Render an explicit template body with the provided model without touching
        /// the persistent store (used by the admin preview feature).
        /// </summary>
        Task<string> RenderContentAsync(string content, object model, CancellationToken cancellationToken = default);
    }
}
