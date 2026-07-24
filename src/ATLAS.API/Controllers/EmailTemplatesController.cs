//----------------------
// EmailTemplates Controller Adapter
// Implements EmailTemplatesControllerBase using MediatR.
//----------------------

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ATLAS.API.Controllers.Generated;
using ATLAS.API.Contracts.Generated;
using ATLAS.Application.EmailTemplates.Queries;
using ATLAS.Application.EmailTemplates.Commands;

namespace ATLAS.API.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public sealed class EmailTemplatesController : EmailTemplatesControllerBase
    {
        private readonly IMediator _mediator;

        [ActivatorUtilitiesConstructor]
        public EmailTemplatesController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public override async Task<ActionResult<ICollection<EmailTemplateResponse>>> GetEmailTemplates()
        {
            var result = await _mediator.Send(new GetEmailTemplatesQuery(), default);
            var response = result.Select(t => t.ToResponse()).ToList();
            return Ok(response);
        }

        public override async Task<ActionResult<EmailTemplateResponse>> GetEmailTemplateByName(string name)
        {
            var result = await _mediator.Send(new GetEmailTemplateByNameQuery(name), default);
            if (result is null)
                return NotFound();
            return Ok(result.ToResponse());
        }

        public override async Task<ActionResult<bool>> UpdateEmailTemplate(string name, UpdateEmailTemplateRequest body)
        {
            if (body is null)
                return BadRequest();

            var updated = await _mediator.Send(new UpdateEmailTemplateCommand(name, body.Content), default);
            if (!updated)
                return NotFound();
            return Ok(updated);
        }

        public override async Task<ActionResult<string>> PreviewEmailTemplate(string name, PreviewEmailTemplateRequest body)
        {
            if (body is null)
                return BadRequest();

            var rendered = await _mediator.Send(new PreviewEmailTemplateQuery(body.Content), default);
            return Ok(rendered);
        }
    }
}
