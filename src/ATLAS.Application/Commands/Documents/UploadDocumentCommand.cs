using MediatR;
using System;
using System.IO;
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
        public Stream FileContent { get; set; } = Stream.Null;
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }

    public class UploadDocumentCommandHandler : IRequestHandler<UploadDocumentCommand, bool>
    {
        private readonly IApplicationRepository _repository;
        private readonly IPermitTypeRepository _permitTypeRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public UploadDocumentCommandHandler(
            IApplicationRepository repository,
            IPermitTypeRepository permitTypeRepository,
            IFileStorageService fileStorageService,
            IMediator mediator,
            ICurrentUserService currentUserService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
            _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
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

            // Step 1-2: Load application and verify exists
            var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
            if (application == null)
                return false;

            // Step 3: Verify ownership — citizens can only upload to own applications
            if (application.CitizenId != uploadedById)
                throw new UnauthorizedAccessException("You can only upload documents to your own applications.");

            // Step 4: Validate against DocumentRequirement if permit type defines any
            var permitType = await _permitTypeRepository.GetByIdAsync(application.PermitTypeId, cancellationToken);
            if (permitType?.DocumentRequirements.Count > 0)
            {
                var ext = Path.GetExtension(request.FileName);
                var matched = permitType.DocumentRequirements
                    .FirstOrDefault(r => r.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase));

                if (matched == null)
                    throw new InvalidOperationException(
                        $"File type '{ext}' is not accepted for this permit type. " +
                        $"Accepted types: {string.Join(", ", permitType.DocumentRequirements.SelectMany(r => r.AllowedExtensions))}");

                if (request.FileSize > matched.MaxFileSizeBytes)
                    throw new InvalidOperationException(
                        $"File size {request.FileSize} bytes exceeds the maximum of {matched.MaxFileSizeBytes} bytes for {matched.DocumentType}.");
            }

            // Step 5: Upload file via IFileStorageService with ADR-015 naming
            var documentId = Guid.NewGuid();
            var blobPath = $"{request.ApplicationId}/{documentId}/{request.FileName}";
            var uploadResult = await _fileStorageService.UploadAsync(
                request.FileContent, blobPath, request.ContentType, cancellationToken);

            // Step 6-7: Create Document entity and attach to aggregate
            var document = application.AddDocument(
                documentId, request.FileName, request.ContentType,
                request.FileSize, uploadResult.BlobUrl, uploadedById);

            // Step 8: Persist changes
            await _repository.UpdateAsync(application, cancellationToken);

            // Step 9: Publish domain event exactly once (not in aggregate — handler manages publication)
            await _mediator.Publish(
                new DocumentUploadedEvent(document.Id, request.ApplicationId, uploadedById, request.FileName),
                cancellationToken);

            return true;
        }
    }
}