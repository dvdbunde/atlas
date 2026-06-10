using MediatR;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries
{
    public class GetApplicationByIdQuery : IRequest<ApplicationDetailDto?>
    {
        public Guid ApplicationId { get; set; }
    }

    public class GetApplicationByIdQueryHandler : IRequestHandler<GetApplicationByIdQuery, ApplicationDetailDto?>
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPermitTypeRepository _permitTypeRepository;

        public GetApplicationByIdQueryHandler(
            IApplicationRepository applicationRepository,
            IUserRepository userRepository,
            IPermitTypeRepository permitTypeRepository)
        {
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
        }

        public async Task<ApplicationDetailDto?> Handle(GetApplicationByIdQuery request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
            
            if (application == null)
                return null;

            // Fetch related data for DTO fields
            var citizen = await _userRepository.GetByIdAsync(application.CitizenId, cancellationToken);
            var permitType = await _permitTypeRepository.GetByIdAsync(application.PermitTypeId, cancellationToken);
            
            // Get officer name from the latest review (if any)
            var latestReview = application.Reviews.OrderByDescending(r => r.ReviewedDate).FirstOrDefault();
            string? officerName = null;
            if (latestReview != null)
            {
                var officer = await _userRepository.GetByIdAsync(latestReview.OfficerId, cancellationToken);
                officerName = officer?.GetFullName();
            }

            return new ApplicationDetailDto
            {
                Id = application.Id,
                ApplicationNumber = application.ApplicationNumber,
                Status = (int)application.Status,
                SubmittedDate = application.SubmittedDate,
                CitizenId = application.CitizenId,
                PermitTypeId = application.PermitTypeId,
                CitizenName = citizen?.GetFullName() ?? "Unknown",
                PermitTypeName = permitType?.Name ?? "Unknown",
                OfficerName = officerName ?? "Not assigned",
                ReviewedDate = application.ReviewedDate,
                CitizenNotes = application.CitizenNotes,
                OfficerNotes = application.OfficerNotes,
                Documents = application.Documents.Select(d => new DocumentDto
                {
                    Id = d.Id,
                    FileName = d.FileName,
                    ContentType = d.ContentType,
                    FileSize = d.FileSize,
                    BlobUrl = d.BlobUrl,
                    UploadedDate = d.UploadedDate,
                    UploadedById = d.UploadedById
                }).ToList(),
                Reviews = application.Reviews.Select(r => new ReviewDto
                {
                    Id = r.Id,
                    OfficerId = r.OfficerId,
                    Decision = (int)r.Decision,
                    ReasonCode = r.ReasonCode,
                    Comments = r.Comments,
                    ReviewedDate = r.ReviewedDate,
                    IsVisibleToCitizen = r.IsVisibleToCitizen
                }).ToList()
            };
        }
    }
}
