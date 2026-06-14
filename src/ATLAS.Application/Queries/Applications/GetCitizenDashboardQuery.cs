using MediatR;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using ATLAS.Application.Interfaces;

namespace ATLAS.Application.Queries.Applications
{
    public class GetCitizenDashboardQuery : IRequest<IEnumerable<CitizenDashboardDto>>
    {
        // No parameters - uses current user context
    }

    public class GetCitizenDashboardQueryHandler : IRequestHandler<GetCitizenDashboardQuery, IEnumerable<CitizenDashboardDto>>
    {
        private readonly IApplicationRepository _repository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetCitizenDashboardQueryHandler> _logger;

        public GetCitizenDashboardQueryHandler(
            IApplicationRepository repository,
            ICurrentUserService currentUserService,
            ILogger<GetCitizenDashboardQueryHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<CitizenDashboardDto>> Handle(GetCitizenDashboardQuery request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.UserId.HasValue)
                throw new UnauthorizedAccessException("User must be authenticated to view dashboard");

            var citizenId = _currentUserService.UserId.Value;
            var applications = await _repository.GetByCitizenIdAsync(citizenId, cancellationToken);

            // Load all active permit types to map PermitTypeId -> Name
            var permitTypes = await _repository.GetActivePermitTypesAsync(cancellationToken);
            var permitTypeDict = permitTypes.ToDictionary(p => p.Id, p => p.Name);

            var dtos = new List<CitizenDashboardDto>();
            foreach (var app in applications)
            {
                permitTypeDict.TryGetValue(app.PermitTypeId, out var permitTypeName);
                dtos.Add(new CitizenDashboardDto
                {
                    ApplicationId = app.Id,
                    ApplicationNumber = app.ApplicationNumber,
                    PermitTypeName = permitTypeName ?? "Unknown",
                    Status = app.Status.ToString(),
                    SubmittedDate = app.SubmittedDate,
                    LastUpdated = app.ReviewedDate ?? app.SubmittedDate
                });
            }

            _logger.LogInformation("Retrieved {Count} applications for citizen {CitizenId}", 
                dtos.Count, citizenId);

            return dtos;
        }
    }
}
