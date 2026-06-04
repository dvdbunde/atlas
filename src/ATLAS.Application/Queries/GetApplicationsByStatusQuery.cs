using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries
{
    public class GetApplicationsByStatusQuery : IRequest<List<ApplicationSummaryDto>>
    {
        public ApplicationStatus? Status { get; set; }
    }

    public class GetApplicationsByStatusQueryHandler : IRequestHandler<GetApplicationsByStatusQuery, List<ApplicationSummaryDto>>
    {
        private readonly IApplicationRepository _repository;

        public GetApplicationsByStatusQueryHandler(IApplicationRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<List<ApplicationSummaryDto>> Handle(GetApplicationsByStatusQuery request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var applications = request.Status.HasValue 
                ? await _repository.GetByStatusAsync(request.Status.Value, cancellationToken)
                : await _repository.GetAllAsync(cancellationToken);

            return applications.Select(app => new ApplicationSummaryDto
            {
                Id = app.Id,
                ApplicationNumber = app.ApplicationNumber,
                Status = (int)app.Status,
                SubmittedDate = app.SubmittedDate,
                CitizenId = app.CitizenId,
                PermitTypeId = app.PermitTypeId
            }).ToList();
        }
    }
}
