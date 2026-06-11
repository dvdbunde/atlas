using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
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
    }

    public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, bool>
    {
        private readonly IApplicationRepository _repository;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public UploadDocumentCommandHandler(
            IApplicationRepository repository,
            IMediator mediator,
            ICurrentUserService currentUserService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<bool> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!_currentUserService.UserId.HasValue)
                throw new UnauthorizedAccessException("Authenticated user must have a valid UserId to upload a document.");

            var uploadedById = _currentUserService.UserId.Value;
            var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
            
            if (application == null)
                return false;

            var document = application.AddDocument(Guid.NewGuid(), request.FileName, request.ContentType, request.FileSize, request.BlobUrl, uploadedById);
            await _repository.UpdateAsync(application, cancellationToken);
            await _mediator.Publish(new DocumentUploadedEvent(document.Id, request.ApplicationId, uploadedById, request.FileName), cancellationToken);
            
            return true;
        }
    }
}