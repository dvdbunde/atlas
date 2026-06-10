using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Commands
{
    public class UploadDocumentCommand : IRequest<bool>
    {
        public Guid ApplicationId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string BlobUrl { get; set; } = string.Empty;
        public Guid UploadedById { get; set; }
    }

    public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, bool>
    {
        private readonly IApplicationRepository _repository;
        private readonly IMediator _mediator;

        public UploadDocumentCommandHandler(IApplicationRepository repository, IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<bool> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
            
            if (application == null)
                return false;

            var document = application.AddDocument(Guid.NewGuid(), request.FileName, request.ContentType, request.FileSize, request.BlobUrl, request.UploadedById);
            await _repository.UpdateAsync(application, cancellationToken);
            await _mediator.Publish(new DocumentUploadedEvent(document.Id, request.ApplicationId, request.UploadedById, request.FileName), cancellationToken);
            
            return true;
        }
    }
}
