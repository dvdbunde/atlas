using ATLAS.Application.DTOs;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ATLAS.Application.Queries.Applications;

/// <summary>
/// Query to retrieve a chronological activity feed for an application.
/// Composed from existing domain data — never a separate persistence model.
/// </summary>
public class GetApplicationActivityQuery : IRequest<IReadOnlyList<ApplicationActivityDto>>
{
    public Guid ApplicationId { get; set; }
}

public class GetApplicationActivityQueryHandler
    : IRequestHandler<GetApplicationActivityQuery, IReadOnlyList<ApplicationActivityDto>>
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IUserRepository _userRepository;

    public GetApplicationActivityQueryHandler(
        IApplicationRepository applicationRepository,
        IUserRepository userRepository)
    {
        _applicationRepository = applicationRepository ?? throw new ArgumentNullException(nameof(applicationRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<IReadOnlyList<ApplicationActivityDto>> Handle(
        GetApplicationActivityQuery request, CancellationToken cancellationToken)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken);
        if (application == null)
            return Array.Empty<ApplicationActivityDto>();

        var activities = new List<ApplicationActivityDto>();
        var resolvedNames = new Dictionary<Guid, string>(); // cache user lookups

        // 1. Application creation
        activities.Add(new ApplicationActivityDto
        {
            Timestamp = application.CreatedDate,
            ActivityType = "Created",
            Title = "Application Created",
            Description = $"Application {application.ApplicationNumber} was created"
        });

        // 2. Submission
        if (application.SubmittedDate.HasValue)
        {
            activities.Add(new ApplicationActivityDto
            {
                Timestamp = application.SubmittedDate.Value,
                ActivityType = "Submitted",
                Title = "Application Submitted",
                Description = "Application was submitted for review"
            });
        }

        // 3. Reviews (decisions)
        foreach (var review in application.Reviews.OrderBy(r => r.ReviewedDate))
        {
            var officerName = await ResolveUserName(review.OfficerId, resolvedNames, cancellationToken);

            var (type, title, description) = review.Decision switch
            {
                ReviewDecision.Approve => ("Approved", "Application Approved", "Application was approved"),
                ReviewDecision.Reject => ("Rejected", "Application Rejected", $"Reason: {review.ReasonCode}"),
                ReviewDecision.RequestInfo => ("InfoRequested", "Information Requested", review.Comments),
                _ => ("Review", "Review Recorded", "")
            };

            activities.Add(new ApplicationActivityDto
            {
                Timestamp = review.ReviewedDate,
                ActivityType = type,
                Title = title,
                Description = description,
                PerformedBy = officerName,
                PerformedByRole = "Officer"
            });
        }

        // 4. Documents
        foreach (var doc in application.Documents)
        {
            var uploaderName = await ResolveUserName(doc.UploadedById, resolvedNames, cancellationToken);

            activities.Add(new ApplicationActivityDto
            {
                Timestamp = doc.UploadedDate,
                ActivityType = "DocumentUploaded",
                Title = "Document Uploaded",
                Description = doc.FileName,
                PerformedBy = uploaderName
            });
        }

        // 5. Assignment (inferred from AssignedDate + AssignedOfficerId)
        if (application.AssignedDate.HasValue && application.AssignedOfficerId.HasValue)
        {
            var officerName = await ResolveUserName(application.AssignedOfficerId.Value, resolvedNames, cancellationToken);

            activities.Add(new ApplicationActivityDto
            {
                Timestamp = application.AssignedDate.Value,
                ActivityType = "Assigned",
                Title = "Officer Assigned",
                Description = $"Application assigned to {officerName}",
                PerformedBy = officerName,
                PerformedByRole = "Officer"
            });
        }

        // 6. Resubmission (inferred — no explicit timestamp field, so use the latest review date or last modified)
        // If there are RequestInfo reviews and the status is UnderReview, a resubmission occurred.
        // Use CreatedDate as a fallback; the ResubmittedEvent timestamp is not stored on the entity.
        // For a more accurate timestamp, consider adding a ResubmittedDate field in a future milestone.
        if (application.Reviews.Any(r => r.Decision == ReviewDecision.RequestInfo)
            && application.Status == ApplicationStatus.UnderReview)
        {
            var lastReview = application.Reviews.OrderByDescending(r => r.ReviewedDate).First();
            activities.Add(new ApplicationActivityDto
            {
                Timestamp = lastReview.ReviewedDate.AddSeconds(1), // slight offset after the info request
                ActivityType = "Resubmitted",
                Title = "Application Resubmitted",
                Description = "Application was updated and resubmitted by the citizen"
            });
        }

        // Sort descending by timestamp
        activities.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));

        return activities;
    }

    private async Task<string> ResolveUserName(Guid userId, Dictionary<Guid, string> cache, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(userId, out var cached))
            return cached;

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        var name = user?.GetFullName() ?? "Unknown";
        cache[userId] = name;
        return name;
    }
}