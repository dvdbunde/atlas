using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;

namespace ATLAS.Application.Queries.Admin
{
    public enum UserSortOption
    {
        NameAsc,
        NameDesc,
        EmailAsc,
        EmailDesc,
        LastLoginDesc
    }

    /// <summary>
    /// Query to retrieve a paged, filtered, sorted summary of users.
    /// Users are a synchronized, read-only projection of Entra ID principals
    /// (see ADR-013); this query never mutates identity data.
    /// </summary>
    public class GetUsersQuery : IRequest<UserListResult>
    {
        public string? SearchTerm { get; set; }
        public UserRole? Role { get; set; }
        public UserSortOption SortBy { get; set; } = UserSortOption.NameAsc;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>Paged result wrapper for the user list.</summary>
    public class UserListResult
    {
        public IReadOnlyList<UserSummaryDto> Items { get; init; } = Array.Empty<UserSummaryDto>();
        public int TotalCount { get; init; }
        public int PageNumber { get; init; }
        public int PageSize { get; init; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }

    public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, UserListResult>
    {
        private readonly IUserRepository _repository;

        public GetUsersQueryHandler(IUserRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public async Task<UserListResult> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            var users = (await _repository.GetAllAsync(cancellationToken)).ToList();

            // Filter by role when specified
            if (request.Role.HasValue)
                users = users.Where(u => u.Role == request.Role.Value).ToList();

            // Search by name or email (case-insensitive, contains)
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim();
                users = users.Where(u =>
                    u.GetFullName().Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Sort
            users = request.SortBy switch
            {
                UserSortOption.NameDesc => users.OrderByDescending(u => u.GetFullName()).ToList(),
                UserSortOption.EmailAsc => users.OrderBy(u => u.Email).ToList(),
                UserSortOption.EmailDesc => users.OrderByDescending(u => u.Email).ToList(),
                UserSortOption.LastLoginDesc => users.OrderByDescending(u => u.LastLoginDate).ToList(),
                _ => users.OrderBy(u => u.GetFullName()).ToList()
            };

            var totalCount = users.Count;
            var pageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
            var pageSize = request.PageSize < 1 ? 20 : request.PageSize;
            var paged = users
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var dtos = paged.Select(u => new UserSummaryDto
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                FullName = u.GetFullName(),
                Email = u.Email,
                Role = u.Role,
                LastLoginDate = u.LastLoginDate
            }).ToList();

            return new UserListResult
            {
                Items = dtos,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
    }
}
