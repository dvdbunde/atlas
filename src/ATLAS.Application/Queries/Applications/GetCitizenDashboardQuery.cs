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
using Microsoft.Extensions.Logging;
using ATLAS.Application.Interfaces;

namespace ATLAS.Application.Queries.Applications
{
    public enum CitizenDashboardSortBy
    {
        LastUpdated,
        SubmittedDate,
        ApplicationNumber
    }

    public class GetCitizenDashboardQuery : IRequest<IEnumerable<CitizenDashboardDto>>
    {
        public Guid? PermitTypeId { get; set; }
        public ApplicationStatus? Status { get; set; }
        public CitizenDashboardSortBy SortBy { get; set; } = CitizenDashboardSortBy.LastUpdated;
        public bool SortDescending { get; set; } = true;
    }

    public class GetCitizenDashboardQueryHandler : IRequestHandler<GetCitizenDashboardQuery, IEnumerable<CitizenDashboardDto>>
    {
        private readonly IApplicationRepository _repository;
        private readonly IPermitTypeRepository _permitTypeRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetCitizenDashboardQueryHandler> _logger;

        public GetCitizenDashboardQueryHandler(
            IApplicationRepository repository,
            IPermitTypeRepository permitTypeRepository,
            ICurrentUserService currentUserService,
            ILogger<GetCitizenDashboardQueryHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<CitizenDashboardDto>> Handle(GetCitizenDashboardQuery request, CancellationToken cancellationToken)
        {
            if (!_currentUserService.UserId.HasValue)
                throw new UnauthorizedAccessException("User must be authenticated to view dashboard");

            var citizenId = _currentUserService.UserId.Value;
            var applications = await _repository.GetByCitizenIdAsync(citizenId, cancellationToken);

            // Filtering/sorting is always scoped to the authenticated citizen's own applications.
            var filtered = applications
                .Where(a => request.PermitTypeId is null || a.PermitTypeId == request.PermitTypeId)
                .Where(a => request.Status is null || a.Status == request.Status)
                .ToList();

            var dtos = new List<CitizenDashboardDto>();
            foreach (var app in filtered)
            {
                var permitTypeName = await _permitTypeRepository.GetNameByIdAsync(app.PermitTypeId, cancellationToken);
                dtos.Add(new CitizenDashboardDto
                {
                    ApplicationId = app.Id,
                    ApplicationNumber = app.ApplicationNumber,
                    PermitTypeName = permitTypeName ?? "Unknown",
                    Status = app.Status,
                    SubmittedDate = app.SubmittedDate,
                    LastUpdated = app.ModifiedDate
                });
            }

            var sorted = SortDtos(dtos, request.SortBy, request.SortDescending);

            _logger.LogInformation("Retrieved {Count} applications for citizen {CitizenId}", sorted.Count, citizenId);

            return sorted;
        }

        private static List<CitizenDashboardDto> SortDtos(
            List<CitizenDashboardDto> dtos,
            CitizenDashboardSortBy sortBy,
            bool descending)
        {
            var sorted = dtos.ToList();

            switch (sortBy)
            {
                case CitizenDashboardSortBy.SubmittedDate:
                    sorted = descending
                        ? sorted.OrderByDescending(d => d.SubmittedDate).ToList()
                        : sorted.OrderBy(d => d.SubmittedDate).ToList();
                    break;
                case CitizenDashboardSortBy.ApplicationNumber:
                    sorted = descending
                        ? sorted.OrderByDescending(d => d.ApplicationNumber).ToList()
                        : sorted.OrderBy(d => d.ApplicationNumber).ToList();
                    break;
                case CitizenDashboardSortBy.LastUpdated:
                default:
                    sorted = descending
                        ? sorted.OrderByDescending(d => d.LastUpdated).ToList()
                        : sorted.OrderBy(d => d.LastUpdated).ToList();
                    break;
            }

            return sorted;
        }
    }
}