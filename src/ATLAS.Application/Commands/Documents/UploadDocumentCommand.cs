using MediatR;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Commands.Documents
{
    public class UploadDocumentCommand : ICommand<bool>
    {
        public Guid ApplicationId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
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
        private readonly IVirusScanner _virusScanner;

        public UploadDocumentCommandHandler(
            IApplicationRepository repository,
            IPermitTypeRepository permitTypeRepository,
            IFileStorageService fileStorageService,
            IVirusScanner virusScanner,
            IMediator mediator,
            ICurrentUserService currentUserService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
            _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
            _virusScanner = virusScanner ?? throw new ArgumentNullException(nameof(virusScanner));
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

            // Step 4: Validate DocumentType against DocumentRequirement if permit type defines any
            if (string.IsNullOrWhiteSpace(request.DocumentType))
                throw new InvalidOperationException("DocumentType is required.");

            var permitType = await _permitTypeRepository.GetByIdAsync(application.PermitTypeId, cancellationToken);
            if (permitType?.DocumentRequirements.Count > 0)
            {
                var matched = permitType.DocumentRequirements
                    .FirstOrDefault(r => r.DocumentType.Equals(request.DocumentType, StringComparison.OrdinalIgnoreCase));

                if (matched == null)
                    throw new InvalidOperationException(
                        $"Document type '{request.DocumentType}' is not defined for this permit type. " +
                        $"Accepted types: {string.Join(", ", permitType.DocumentRequirements.Select(r => r.DocumentType))}");

                var ext = Path.GetExtension(request.FileName);
                if (!matched.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        $"File type '{ext}' is not accepted for document type '{request.DocumentType}'. " +
                        $"Accepted extensions: {string.Join(", ", matched.AllowedExtensions)}");

                if (request.FileSize > matched.MaxFileSizeBytes)
                    throw new InvalidOperationException(
                        $"File size {request.FileSize} bytes exceeds the maximum of {matched.MaxFileSizeBytes} bytes for {matched.DocumentType}.");
            }

            // Step 4b: Virus scan (pass-through for MVP)
            var scanResult = await _virusScanner.ScanAsync(
                request.FileContent, request.FileName, cancellationToken);
            if (!scanResult.IsClean)
            {
                throw new InvalidOperationException(
                    $"File rejected by security scan: {scanResult.ThreatName ?? "Unknown threat"}.");
            }

            // Step 5: Upload file via IFileStorageService with ADR-015 naming
            var documentId = Guid.NewGuid();
            var blobPath = $"{request.ApplicationId}/{documentId}/{request.FileName}";

            FileUploadResult uploadResult;
            try
            {
                uploadResult = await _fileStorageService.UploadAsync(
                    request.FileContent, blobPath, request.ContentType, cancellationToken);
            }
            catch (Exception ex) when (ex is not ArgumentException and not ArgumentNullException)
            {
                throw new InvalidOperationException(
                    "An error occurred while uploading the file. Please try again later.", ex);
            }

            // Step 6-7: Create Document entity and attach to aggregate
            var document = application.AddDocument(
                documentId, request.DocumentType, request.FileName, request.ContentType,
                request.FileSize, uploadResult.BlobUrl, uploadedById);

            // Step 8: Persist changes
            await _repository.UpdateAsync(application, cancellationToken);

            // Step 9: Publish domain event exactly once (not in aggregate — handler manages publication)
            await _mediator.Publish(
                new DocumentUploadedEvent(document.Id, request.ApplicationId, request.FileName),
                cancellationToken);

            return true;
        }        
    }
}