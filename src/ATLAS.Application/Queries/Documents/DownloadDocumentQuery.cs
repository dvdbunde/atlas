using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries.Documents
{
    public class DownloadDocumentQuery : IRequest<DownloadDocumentResult?>
    {
        public Guid DocumentId { get; set; }
    }

    /// <summary>
    /// Result of a successful download query.
    /// Contains the short-lived SAS URI — BlobUrl is never exposed directly.
    /// </summary>
    public record DownloadDocumentResult(string SasUri, string FileName, string ContentType);

    public class DownloadDocumentQueryHandler : IRequestHandler<DownloadDocumentQuery, DownloadDocumentResult?>
    {
        private readonly IApplicationRepository _repository;
        private readonly IFileStorageService _fileStorageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMediator _mediator;

        public DownloadDocumentQueryHandler(
            IApplicationRepository repository,
            IFileStorageService fileStorageService,
            ICurrentUserService currentUserService,
            IMediator mediator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _fileStorageService = fileStorageService ?? throw new ArgumentNullException(nameof(fileStorageService));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<DownloadDocumentResult?> Handle(DownloadDocumentQuery request, CancellationToken ct)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!_currentUserService.UserId.HasValue)
                throw new UnauthorizedAccessException("Authentication required.");

            // Load the application that contains this document
            var application = await _repository.GetByDocumentIdAsync(request.DocumentId, ct);
            if (application == null)
                return null;

            // Find the document in the application
            var document = application.Documents.FirstOrDefault(d => d.Id == request.DocumentId);
            if (document == null)
                return null;

            var userId = _currentUserService.UserId.Value;
            var userRole = _currentUserService.Role;

            // Authorization:
            // - Citizen: own applications only
            // - Officer: assigned or unassigned applications
            // - Admin: all applications
            bool isAuthorized = application.CitizenId == userId
                || userRole == "Admin"
                || (userRole == "Officer" && application.Reviews.Any(r => r.OfficerId == userId));

            if (!isAuthorized)
                throw new UnauthorizedAccessException("You do not have permission to download this document.");
            // Generate SAS URI with ADR-015 default 1-hour expiry
            var sasUri = await _fileStorageService.GenerateDownloadSasUriAsync(
                document.BlobUrl, TimeSpan.FromHours(1), ct);

            // Publish audit event
            await _mediator.Publish(
                new DocumentDownloadedEvent(document.Id, application.Id, userId, document.BlobUrl),
                ct);

            return new DownloadDocumentResult(sasUri, document.FileName, document.ContentType);
        }
    }
}