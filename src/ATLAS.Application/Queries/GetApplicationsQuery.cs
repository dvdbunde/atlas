using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries
{
    public class GetApplicationsQuery : IRequest<IEnumerable<ApplicationSummaryDto>>
    {
        public Guid? CitizenId { get; set; }
        public Guid? OfficerId { get; set; }
        public string? Status { get; set; }
        public Guid? PermitTypeId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public string? Search { get; set; }
    }

    public class GetApplicationsQueryHandler : IRequestHandler<GetApplicationsQuery, IEnumerable<ApplicationSummaryDto>>
    {
        private readonly IApplicationRepository _applicationRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPermitTypeRepository _permitTypeRepository;
        private readonly ICurrentUserService _currentUserService;

        public GetApplicationsQueryHandler(
            IApplicationRepository applicationRepository,
            IUserRepository userRepository,
            IPermitTypeRepository permitTypeRepository,
            ICurrentUserService currentUserService)
        {
            _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<IEnumerable<ApplicationSummaryDto>> Handle(GetApplicationsQuery request, CancellationToken cancellationToken)
        {
            // Get all applications (repository returns IEnumerable)
            var applications = await _applicationRepository.GetAllAsync(cancellationToken);
            var query = applications.AsQueryable();

            // Role-based auto-scoping:
            // - Citizens: can only see their own applications (client-supplied citizenId is ignored)
            // - Officers: can only see applications assigned to them (unless admin)
            // - Admins: full access with all client-supplied filters
            var role = _currentUserService.Role;
            var userId = _currentUserService.UserId;

            if (role == "Citizen" && userId.HasValue)
            {
                query = query.Where(a => a.CitizenId == userId.Value);
            }
            // Officer — no Reviews-based auto-scoping in MVP.
            // Authorization is enforced at the endpoint level by
            // GeneratedControllerAuthorizationConvention (OfficerOrAdmin policy).
            // Admin — no auto-scoping; all filters below apply

            // Apply client-supplied filters (for admins/officers with explicit params)
            // Note: citizenId/officerId params are ignored for citizens (already auto-scoped)
            if (request.CitizenId.HasValue && role != "Citizen")
                query = query.Where(a => a.CitizenId == request.CitizenId);
            
            if (request.OfficerId.HasValue && role != "Officer")
                query = query.Where(a => a.Reviews.Any(r => r.OfficerId == request.OfficerId));
            
            if (!string.IsNullOrEmpty(request.Status))
            {
                // Parse status string (comma-separated) to enum
                var statusList = request.Status.Split(',').Select(s => 
                {
                    if (Enum.TryParse<ATLAS.Domain.Enums.ApplicationStatus>(s.Trim(), out var status))
                        return status;
                    return (ATLAS.Domain.Enums.ApplicationStatus?)null;
                }).Where(s => s.HasValue).Select(s => s!.Value);
                
                if (statusList.Any())
                    query = query.Where(a => statusList.Contains(a.Status));
            }
            
            if (request.PermitTypeId.HasValue)
                query = query.Where(a => a.PermitTypeId == request.PermitTypeId);
            
            if (request.DateFrom.HasValue)
                query = query.Where(a => a.SubmittedDate >= request.DateFrom);
            
            if (request.DateTo.HasValue)
                query = query.Where(a => a.SubmittedDate <= request.DateTo);
            
            // TODO: Implement search (by application number or citizen name)
            // This requires joining with User entity
            
            // Execute query and map to DTOs
            var results = query.ToList();
            
            var dtos = new List<ApplicationSummaryDto>();
            foreach (var app in results)
            {
                // TODO: Enrich with CitizenName and PermitTypeName
                // Requires fetching User and PermitType entities
                dtos.Add(new ApplicationSummaryDto
                {
                    Id = app.Id,
                    ApplicationNumber = app.ApplicationNumber,
                    Status = (int)app.Status,
                    SubmittedDate = app.SubmittedDate,
                    CitizenId = app.CitizenId,
                    PermitTypeId = app.PermitTypeId
                });
            }
            
            return dtos;
        }
    }
}
