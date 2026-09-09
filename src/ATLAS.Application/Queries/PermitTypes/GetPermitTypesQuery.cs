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

    /// <summary>
    /// Status filter for the Admin Permit Types list. Replaces the previous pair of
    /// Active-only / Inactive-only checkboxes with a single status selection.
    /// </summary>
    public enum PermitTypeStatusFilter
    {
        All,
        Active,
        Inactive
    }

    public class GetPermitTypesQuery : IRequest<IEnumerable<PermitTypeSummaryDto>>
    {
        public string? SearchTerm { get; set; }
        public PermitTypeStatusFilter StatusFilter { get; set; } = PermitTypeStatusFilter.All;
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

            // Status filter: All shows everything; Active / Inactive narrow the set.
            if (request.StatusFilter == PermitTypeStatusFilter.Active)
                permitTypes = permitTypes.Where(pt => pt.IsActive).ToList();

            if (request.StatusFilter == PermitTypeStatusFilter.Inactive)
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
