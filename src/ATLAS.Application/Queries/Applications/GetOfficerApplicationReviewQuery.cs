using ATLAS.Application.DTOs;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using ATLAS.Domain.ValueObjects;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Application.Queries.Applications;

/// <summary>
/// Read-only officer case review. Returns a single cohesive projection of one
/// application for the officer review page. Never returns the Application aggregate.
/// </summary>
public class GetOfficerApplicationReviewQuery : IRequest<OfficerApplicationReviewDto?>
{
    public Guid ApplicationId { get; set; }
}

public class GetOfficerApplicationReviewQueryHandler
    : IRequestHandler<GetOfficerApplicationReviewQuery, OfficerApplicationReviewDto?>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPermitTypeRepository _permitTypeRepository;

    public GetOfficerApplicationReviewQueryHandler(
        IApplicationRepository applicationRepository,
        IUserRepository userRepository,
        IPermitTypeRepository permitTypeRepository)
    {
        _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _permitTypeRepository = permitTypeRepository ?? throw new ArgumentNullException(nameof(permitTypeRepository));
    }

    public async Task<OfficerApplicationReviewDto?> Handle(
        GetOfficerApplicationReviewQuery request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application == null)
            return null;

        var citizen = await _userRepository.GetByIdAsync(application.CitizenId, cancellationToken);
        var permitType = await _permitTypeRepository.GetByIdAsync(application.PermitTypeId, cancellationToken);

        // Assigned officer (read-only, derived from latest review).
        string? assignedOfficerName = null;
        var latestReview = application.Reviews.OrderByDescending(r => r.ReviewedDate).FirstOrDefault();
        if (latestReview != null)
        {
            var officer = await _userRepository.GetByIdAsync(latestReview.OfficerId, cancellationToken);
            assignedOfficerName = officer?.GetFullName();
        }

        // Submitted dynamic field values, projected with permit field metadata for labels.
        var fieldValues = application.FieldValues
            .OrderBy(fv => fv.SortOrder)
            .ThenBy(fv => fv.FieldName)
            .Select(fv =>
            {
                var meta = permitType?.Fields.FirstOrDefault(pf => pf.Name == fv.FieldName);
                return new OfficerFieldValueDto
                {
                    FieldName = fv.FieldName,
                    Label = meta?.Name ?? fv.FieldName,
                    Value = fv.Value,
                    FieldType = meta?.Type ?? FieldType.Text
                };
            })
            .ToList();

        // Requirement-centric document projection, keyed by the persisted DocumentType.
        var uploadedByType = application.Documents
            .GroupBy(d => d.DocumentType, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var requirements = (permitType?.DocumentRequirements ?? Enumerable.Empty<DocumentRequirement>())
            .Select(req =>
            {
                uploadedByType.TryGetValue(req.DocumentType, out var docs);
                var uploaded = (docs ?? new List<Domain.Entities.Document>())
                    .Select(d => new OfficerDocumentDto
                    {
                        Id = d.Id,
                        FileName = d.FileName,
                        ContentType = d.ContentType,
                        FileSize = d.FileSize,
                        UploadedDate = d.UploadedDate
                    })
                    .ToList();
                return new OfficerDocumentRequirementDto
                {
                    DocumentType = req.DocumentType,
                    IsRequired = req.IsRequired,
                    UploadedDocuments = uploaded,
                    IsSatisfied = uploaded.Count > 0
                };
            })
            .ToList();

        // Existing reviews (read-only).
        var reviews = application.Reviews
            .OrderBy(r => r.ReviewedDate)
            .Select(r => new OfficerReviewDto
            {
                Id = r.Id,
                OfficerId = r.OfficerId,
                Decision = r.Decision,
                ReasonCode = r.ReasonCode,
                Comments = r.Comments,
                ReviewedDate = r.ReviewedDate
            })
            .ToList();

        return new OfficerApplicationReviewDto
        {
            ApplicationId = application.Id,
            ApplicationNumber = application.ApplicationNumber,
            Status = application.Status,
            PermitTypeName = permitType?.Name ?? "Unknown",
            PermitTypeDescription = permitType?.Description ?? string.Empty,
            SubmittedDate = application.SubmittedDate,
            LastUpdated = application.ReviewedDate ?? application.SubmittedDate,
            CitizenId = application.CitizenId,
            CitizenName = citizen?.GetFullName() ?? "Unknown",
            CitizenEmail = citizen?.Email ?? string.Empty,
            AssignedOfficerName = assignedOfficerName,
            CitizenNotes = application.CitizenNotes,
            FieldValues = fieldValues,
            DocumentRequirements = requirements,
            Reviews = reviews
        };
    }
}