using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries
{
    public class GetApplicationsByOfficerIdQuery : IRequest<List<ApplicationSummaryDto>>
    {
        public Guid OfficerId { get; set; }
    }

    public class GetApplicationsByOfficerIdQueryHandler : IRequestHandler<GetApplicationsByOfficerIdQuery, List<ApplicationSummaryDto>>
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPermitTypeRepository _permitTypeRepository;

        public GetApplicationsByOfficerIdQueryHandler(
            IApplicationRepository applicationRepository,
            IUserRepository userRepository,
            IPermitTypeRepository permitTypeRepository)
        {
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
        }

        public async Task<List<ApplicationSummaryDto>> Handle(GetApplicationsByOfficerIdQuery request, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var applications = await _applicationRepository.GetByOfficerIdAsync(request.OfficerId, cancellationToken);
            
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
