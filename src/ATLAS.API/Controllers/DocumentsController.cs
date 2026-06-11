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
            var command = new UploadDocumentCommand
            {
                ApplicationId = applicationId,
                FileName = body.FileName,
                ContentType = body.ContentType,
                FileSize = body.FileSize,
                BlobUrl = body.BlobUrl?.ToString() ?? string.Empty
            };

            var result = await _mediator.Send(command, default);                        
            if (!result)
            {
                return NotFound(); // ← Application not found
            }
    
            return NoContent(); // ← 204 for successful upload
        }     

        public override async Task<IActionResult> Download(Guid documentId)
        {
            // TODO: Implement document download
            // Return 501 Not Implemented
            return StatusCode(501);
        }
    }
}