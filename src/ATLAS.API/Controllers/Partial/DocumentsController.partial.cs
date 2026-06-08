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
    public partial class DocumentsController : ControllerBase, IDocumentsController
    {
        private readonly IMediator _mediator;

        public DocumentsController(IMediator mediator)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> DocumentsAsync(
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

            return await _mediator.Send(command, default);
        }

        public async Task<FileResult> DownloadAsync(
            Guid documentId)
        {
            // TODO: Implement document download
            throw new System.NotImplementedException("Document download not yet implemented");
        }
    }
}
