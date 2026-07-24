using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries.PermitTypes
{
    public enum PermitTypeSortOption
    {
        NameAsc,
        NameDesc
    }

    public class GetPermitTypesQuery : IRequest<IEnumerable<PermitTypeSummaryDto>>
    {
        public bool IncludeInactive { get; set; } = false;
        public string? SearchTerm { get; set; }
        public bool ActiveOnly { get; set; }
        public bool InactiveOnly { get; set; }
        public PermitTypeSortOption SortBy { get; set; } = PermitTypeSortOption.NameAsc;
    }

    public class GetPermitTypesQueryHandler : IRequestHandler<GetPermitTypesQuery, IEnumerable<PermitTypeSummaryDto>>
    {
        private readonly IPermitTypeRepository _repository;

        public GetPermitTypesQueryHandler(IPermitTypeRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<IEnumerable<PermitTypeSummaryDto>> Handle(GetPermitTypesQuery request, CancellationToken cancellationToken)
        {
            var permitTypes = (await _repository.GetAllAsync(cancellationToken)).ToList();

            // Filter out inactive if not explicitly requested
            if (!request.IncludeInactive)
                permitTypes = permitTypes.Where(pt => pt.IsActive).ToList();

            // Active / Inactive filters (only when explicitly requested)
            if (request.ActiveOnly)
                permitTypes = permitTypes.Where(pt => pt.IsActive).ToList();

            if (request.InactiveOnly)
                permitTypes = permitTypes.Where(pt => !pt.IsActive).ToList();

            // Search by name (case-insensitive, contains)
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                permitTypes = permitTypes
                    .Where(pt => pt.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Sort by name
            permitTypes = request.SortBy == PermitTypeSortOption.NameDesc
                ? permitTypes.OrderByDescending(pt => pt.Name).ToList()
                : permitTypes.OrderBy(pt => pt.Name).ToList();

            // Map to PermitTypeSummaryDto
            var dtos = permitTypes.Select(pt => new PermitTypeSummaryDto
            {
                Id = pt.Id,
                Name = pt.Name,
                Description = pt.Description,
                Fee = pt.Fee,
                IsActive = pt.IsActive,
                FieldCount = pt.Fields.Count,
                DocumentRequirementCount = pt.DocumentRequirements.Count
            }).ToList();

            return dtos;
        }
    }
}
