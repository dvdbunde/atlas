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

namespace ATLAS.API.Controllers.Generated
{
    public partial class DocumentsController : ControllerBase, IDocumentsController
    {
        private readonly IMediator _mediator;

        [ActivatorUtilitiesConstructor]
        public DocumentsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _implementation = this;
        }

        public async Task<ActionResult<bool>> DocumentsAsync(
            Guid applicationId,
            UploadDocumentRequest body)
        {
            var command = new UploadDocumentCommand
            {
                ApplicationId = applicationId,
                FileName = body.FileName,
                ContentType = body.ContentType,
                FileSize = body.FileSize,
                BlobUrl = body.BlobUrl?.ToString() ?? string.Empty,
                UploadedById = body.UploadedById
            };

            var result = await _mediator.Send(command, default);
            return Ok(result);
        }

        public async Task<IActionResult> DownloadAsync(Guid documentId)
        {
            // TODO: Implement document download
            // Return 501 Not Implemented
            return StatusCode(501);
        }
    }
}