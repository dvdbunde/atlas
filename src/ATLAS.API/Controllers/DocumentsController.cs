using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ATLAS.Application.Commands;
using ATLAS.Application.Queries;
using Microsoft.AspNetCore.Http;
using ATLAS.Domain;
using ATLAS.API.Requests;

namespace ATLAS.API.Controllers
{
    [ApiController]
    [Route("api/applications/{applicationId}/documents")]
    [Produces("application/json")]
    public class DocumentsController : ControllerBase
    {
        private readonly IMediator _mediator;

        public DocumentsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        #region POST /api/applications/{applicationId}/documents (F-03)
        [HttpPost]
        [Authorize(Roles = "Citizen,Officer,Admin")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> UploadDocument(
            [FromRoute] Guid applicationId,
            [FromBody] UploadDocumentRequest request,
            CancellationToken cancellationToken)
        {
            // Map API Request → MediatR Command
            var command = new UploadDocumentCommand
            {
                ApplicationId = applicationId,
                FileName = request.FileName,
                ContentType = request.ContentType,
                FileSize = request.FileSize,
                BlobUrl = request.BlobUrl,
                UploadedById = request.UploadedById
            };

            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (DomainException ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Invalid File",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = ex.Message
                });
            }
        }
        #endregion

        #region GET /api/documents/{documentId}/download (F-08)
        [HttpGet("../../documents/{documentId}/download")]
        [Authorize(Roles = "Citizen,Officer,Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadDocument(
            [FromRoute] Guid documentId,
            CancellationToken cancellationToken)
        {
            // TODO: Create GetDocumentByIdQuery and handler
            // TODO: Generate SAS token for Azure Blob Storage
            // TODO: Return FileResult with Blob stream

            throw new NotImplementedException("Document download not yet implemented");
        }
        #endregion
    }
}