//----------------------
// Documents Controller Adapter
// Implements IDocumentsController using MediatR
//----------------------

#nullable enable

using MediatR;
using Microsoft.AspNetCore.Mvc;
using ATLAS.API.Controllers.Generated;
using ATLAS.API.Contracts.Generated;
using ATLAS.Application.Commands;
using System.Threading.Tasks;

namespace ATLAS.API.Controllers
{
    [ApiController]    
    [Produces("application/json")]
    public sealed class DocumentsController : DocumentsControllerBase
    {
        private readonly IMediator _mediator;

        [ActivatorUtilitiesConstructor]
        public DocumentsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public override async Task<ActionResult<bool>> Documents(
            Guid applicationId,
            UploadDocumentRequest body)
        {
            // Map from request — file stream, metadata extracted by model binding
            // Note: UploadDocumentRequest contract will need to carry file metadata
            // Controller extracts file content from the request (IFormFile or base64)
            // and passes Stream to the command — no Infrastructure coupling.
            
            var command = new UploadDocumentCommand
            {
                ApplicationId = applicationId,
                FileContent = body.FileContent is { Length: > 0 }
                    ? new MemoryStream(body.FileContent)
                    : Stream.Null,
                FileName = body.FileName,
                ContentType = body.ContentType,
                FileSize = body.FileSize
            };

            var result = await _mediator.Send(command, default);

            if (!result)
                return NotFound();

            return NoContent();
        }

        public override async Task<IActionResult> Download(Guid documentId)
        {
            // TODO: Implement document download
            return StatusCode(501);
        }
    }
}