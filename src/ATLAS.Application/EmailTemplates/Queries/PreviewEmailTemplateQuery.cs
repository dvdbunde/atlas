//----------------------
// PreviewEmailTemplateQuery — render arbitrary content with a fixed sample model.
// Reuses IEmailTemplateRenderer.RenderContentAsync; no separate preview renderer.
//----------------------

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.EmailTemplates;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Enums;
using MediatR;

namespace ATLAS.Application.EmailTemplates.Queries
{
    public class PreviewEmailTemplateQuery : IRequest<string>
    {
        public PreviewEmailTemplateQuery(string content)
        {
            Content = content;
        }

        public string Content { get; }
    }

    public class PreviewEmailTemplateQueryHandler : IRequestHandler<PreviewEmailTemplateQuery, string>
    {
        private readonly IEmailTemplateRenderer _renderer;

        public PreviewEmailTemplateQueryHandler(IEmailTemplateRenderer renderer)
        {
            _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        public Task<string> Handle(PreviewEmailTemplateQuery request, CancellationToken cancellationToken)
        {
            // Fixed sample model so administrators see realistic substitution output.
            var sample = new ApplicationSummaryDto
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                ApplicationNumber = "APP-2025-000123",
                Status = ApplicationStatus.UnderReview,
                SubmittedDate = new DateTime(2025, 1, 15, 9, 30, 0, DateTimeKind.Utc),
                CitizenId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                PermitTypeId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                CitizenName = "Jane Citizen",
                PermitTypeName = "Residential Building Permit"
            };

            // Message / ReasonCode are supplied by the InfoRequest / Rejection handlers
            // and are not part of ApplicationSummaryDto, so add them to the sample model.
            var model = new
            {
                sample.ApplicationNumber,
                sample.PermitTypeName,
                sample.Status,
                sample.CitizenName,
                Message = "Please provide a site plan showing the proposed setback distances.",
                ReasonCode = "MISSING_DOCUMENT"
            };

            return _renderer.RenderContentAsync(request.Content, model, cancellationToken);
        }
    }
}
