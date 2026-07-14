//----------------------
// Documents Controller Adapter
// Implements IDocumentsController using MediatR
//----------------------

#nullable enable

using MediatR;
using Microsoft.AspNetCore.Mvc;
using ATLAS.API.Controllers.Generated;
using ATLAS.API.Contracts.Generated;
using ATLAS.Application.Queries.Documents;
using ATLAS.Application.Commands.Documents;
using Microsoft.AspNetCore.Hosting;

namespace ATLAS.API.Controllers
{
    [ApiController]    
    [Produces("application/json")]
    public sealed class DocumentsController : DocumentsControllerBase
    {        
        private readonly IMediator _mediator;
        private readonly IWebHostEnvironment _env;

        [ActivatorUtilitiesConstructor]
        public DocumentsController(IMediator mediator, IWebHostEnvironment env)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }      

        public override async Task<IActionResult> DownloadDocument(Guid documentId)
        {
            var query = new DownloadDocumentQuery { DocumentId = documentId };
            var result = await _mediator.Send(query, default);
        
            if (result == null)
                return NotFound();
        
            // Development (Azurite): proxy through server to avoid CORS issues.
            // Production (Azure Storage): 302 redirect for direct client download.
            if (_env.IsDevelopment())
            {
                using var httpClient = new HttpClient();
                using var blobResponse = await httpClient.GetAsync(result.SasUri);
            
                if (!blobResponse.IsSuccessStatusCode)
                    return StatusCode((int)blobResponse.StatusCode,
                        "Failed to retrieve document from storage.");
            
                var contentBytes = await blobResponse.Content.ReadAsByteArrayAsync();
                return File(contentBytes, result.ContentType, result.FileName);
            }
            return Redirect(result.SasUri);
        }      

        public override async Task<ActionResult<bool>> UploadDocument(Guid applicationId, [FromBody] UploadDocumentRequest body)
        {
            
            // Map from request — file stream, metadata extracted by model binding
            // Note: UploadDocumentRequest contract will need to carry file metadata
            // Controller extracts file content from the request (IFormFile or base64)
            // and passes Stream to the command — no Infrastructure coupling.
            
            var command = new UploadDocumentCommand
            {
                ApplicationId = applicationId,
                DocumentType = body.DocumentType,
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

        public override async Task<IActionResult> DeleteDocument(Guid applicationId, Guid documentId)
        {
            try
            {
                var command = new DeleteDocumentCommand
                {
                    ApplicationId = applicationId,
                    DocumentId = documentId
                };

                await _mediator.Send(command, default);

                return NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}