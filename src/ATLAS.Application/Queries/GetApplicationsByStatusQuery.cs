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
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPermitTypeRepository _permitTypeRepository;

        public GetApplicationsByStatusQueryHandler(
            IApplicationRepository applicationRepository,
            IUserRepository userRepository,
            IPermitTypeRepository permitTypeRepository)
        {
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
        }

        public async Task<List<ApplicationSummaryDto>> Handle(GetApplicationsByStatusQuery request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var applications = request.Status.HasValue 
                ? await _applicationRepository.GetByStatusAsync(request.Status.Value, cancellationToken)
                : await _applicationRepository.GetAllAsync(cancellationToken);

            var dtos = new List<ApplicationSummaryDto>();
            
            foreach (var app in applications)
            {
                var citizen = await _userRepository.GetByIdAsync(app.CitizenId, cancellationToken);
                var permitType = await _permitTypeRepository.GetByIdAsync(app.PermitTypeId, cancellationToken);
                
                dtos.Add(new ApplicationSummaryDto
                {
                    Id = app.Id,
                    ApplicationNumber = app.ApplicationNumber,
                    Status = (int)app.Status,
                    SubmittedDate = app.SubmittedDate,
                    CitizenId = app.CitizenId,
                    PermitTypeId = app.PermitTypeId,
                    CitizenName = citizen?.GetFullName() ?? "Unknown",
                    PermitTypeName = permitType?.Name ?? "Unknown"
                });
            }
            
            return dtos;
        }
    }
}
