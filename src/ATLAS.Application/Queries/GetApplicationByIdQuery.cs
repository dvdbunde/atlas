using MediatR;
using System;
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
        private readonly IApplicationRepository _repository;

        public GetApplicationByIdQueryHandler(IApplicationRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<ApplicationDetailDto?> Handle(GetApplicationByIdQuery request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken);
            
            if (application == null)
                return null;

            return new ApplicationDetailDto
            {
                Id = application.Id,
                ApplicationNumber = application.ApplicationNumber,
                Status = (int)application.Status,
                SubmittedDate = application.SubmittedDate,
                CitizenId = application.CitizenId,
                PermitTypeId = application.PermitTypeId,
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
                    UploadedDate = d.UploadedDate
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
