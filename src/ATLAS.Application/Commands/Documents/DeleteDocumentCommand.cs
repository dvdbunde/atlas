using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using ATLAS.Application.Interfaces;

namespace ATLAS.Application.Commands.Documents
{
    public class DeleteDocumentCommand : ICommand<Unit>
    {
        public Guid ApplicationId { get; set; }
        public Guid DocumentId { get; set; }
    }

    public class DeleteDocumentCommandHandler : IRequestHandler<DeleteDocumentCommand, Unit>
    {
        private readonly IApplicationRepository _repository;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<DeleteDocumentCommandHandler> _logger;

        public DeleteDocumentCommandHandler(
            IApplicationRepository repository,
            ICurrentUserService currentUserService,
            IFileStorageService fileStorageService,
            ILogger<DeleteDocumentCommandHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileStorageService = fileStorageService;
        }

        public async Task<Unit> Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
            if (application == null)
                throw new ArgumentException($"Application {request.ApplicationId} not found", nameof(request.ApplicationId));

            // Verify ownership
            if (!_currentUserService.UserId.HasValue || application.CitizenId != _currentUserService.UserId.Value)
                throw new UnauthorizedAccessException("You can only delete documents from your own applications.");

            // Verify status
            if (application.Status != Domain.Enums.ApplicationStatus.Draft)
                throw new InvalidOperationException("Documents can only be deleted from draft applications.");

            // Get the blob URL before removing the document
            var document = application.Documents
                .FirstOrDefault(d => d.Id == request.DocumentId);
            if (document is null)
                throw new ArgumentException(
                    $"Document {request.DocumentId} not found in application {request.ApplicationId}",
                    nameof(request.DocumentId));

            // Delete from blob storage (best-effort — log if not found)
            var blobDeleted = await _fileStorageService.DeleteAsync(document.BlobUrl, cancellationToken);
            if (!blobDeleted)
            {
                _logger.LogWarning(
                    "Blob for document {DocumentId} ({BlobUrl}) was not found in storage; removing database record anyway",
                    request.DocumentId,
                    document.BlobUrl);
            }

            // Remove the document from the aggregate
            application.RemoveDocument(request.DocumentId);

            await _repository.UpdateAsync(application, cancellationToken);

            _logger.LogInformation(
                "Document {DocumentId} removed from application {ApplicationId}",
                request.DocumentId,
                request.ApplicationId);

            return Unit.Value;
        }
    }
}