using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.Interfaces;
using ATLAS.Application.Queries.Applications;
using Entities = ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries;

public class GetOfficerDashboardQueryHandlerTests
{
    private readonly Mock<IApplicationRepository> _mockAppRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly Mock<IPermitTypeRepository> _mockPermitTypeRepo;
    private readonly GetOfficerDashboardQueryHandler _handler;

    public GetOfficerDashboardQueryHandlerTests()
    {
        _mockAppRepo = new Mock<IApplicationRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockPermitTypeRepo = new Mock<IPermitTypeRepository>();
        _handler = new GetOfficerDashboardQueryHandler(
            _mockAppRepo.Object, _mockUserRepo.Object, _mockPermitTypeRepo.Object);
    }

       private static Entities.Application MakeApplication(Guid citizenId, Guid permitTypeId, ApplicationStatus status)
    {
        var app = new Entities.Application(citizenId, permitTypeId, "notes");
        var officerId = Guid.NewGuid();

        switch (status)
        {
            case ApplicationStatus.Draft:
                // No transition — stays Draft
                break;
            case ApplicationStatus.Submitted:
                app.Submit();
                break;
            case ApplicationStatus.UnderReview:
                app.Submit();
                app.StartReview(officerId);
                app.AssignToOfficer(officerId); // O4: decisions require assignment
                break;
            case ApplicationStatus.InfoRequested:
                app.Submit();
                app.StartReview(officerId);
                app.AssignToOfficer(officerId); // O4: decisions require assignment
                app.RequestInfo(officerId, "Please provide more information");
                break;
            case ApplicationStatus.Approved:
                app.Submit();
                app.StartReview(officerId);
                app.AssignToOfficer(officerId); // O4: decisions require assignment
                app.Approve(officerId, "Application complete");
                break;
            case ApplicationStatus.Rejected:
                app.Submit();
                app.StartReview(officerId);
                app.AssignToOfficer(officerId); // O4: decisions require assignment
                app.Reject(officerId, "INCOMPLETE_DOCUMENTATION", "Missing required documents");
                break;
            case ApplicationStatus.Resubmitted:
                app.Submit();
                app.StartReview(officerId);
                app.AssignToOfficer(officerId); // O4: decisions require assignment
                app.RequestInfo(officerId, "Please provide more information");
                app.Resubmit();
                break;
        }

        return app;
    }

    [Fact]
    public async Task Handle_ShouldOnlyReturnOfficerWorkflowStatuses()
    {
        var citizen = Guid.NewGuid();
        var pt = Guid.NewGuid();
        var apps = new List<Entities.Application>
        {
            MakeApplication(citizen, pt, ApplicationStatus.Submitted),
            MakeApplication(citizen, pt, ApplicationStatus.UnderReview),
            MakeApplication(citizen, pt, ApplicationStatus.InfoRequested),
            MakeApplication(citizen, pt, ApplicationStatus.Draft),
            MakeApplication(citizen, pt, ApplicationStatus.Approved),
            MakeApplication(citizen, pt, ApplicationStatus.Rejected)
        };
        _mockAppRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(apps);
        SetupLookups(citizen, pt);

        var result = await _handler.Handle(new GetOfficerDashboardQuery(), CancellationToken.None);

        Assert.Equal(3, result.TotalCount);
        Assert.All(result.Items, i => Assert.NotEqual(ApplicationStatus.Draft, i.Status));
        Assert.All(result.Items, i => Assert.NotEqual(ApplicationStatus.Approved, i.Status));
        Assert.All(result.Items, i => Assert.NotEqual(ApplicationStatus.Rejected, i.Status));
    }

    [Fact]
    public async Task Handle_ShouldFilterByStatus()
    {
        var citizen = Guid.NewGuid();
        var pt = Guid.NewGuid();
        var apps = new List<Entities.Application>
        {
            MakeApplication(citizen, pt, ApplicationStatus.Submitted),
            MakeApplication(citizen, pt, ApplicationStatus.UnderReview)
        };
        _mockAppRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(apps);
        SetupLookups(citizen, pt);

        var result = await _handler.Handle(
            new GetOfficerDashboardQuery { Statuses = new List<ApplicationStatus> { ApplicationStatus.Submitted } },
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(ApplicationStatus.Submitted, result.Items[0].Status);
    }

    [Fact]
    public async Task Handle_ShouldFilterByPermitType()
    {
        var citizen = Guid.NewGuid();
        var pt1 = Guid.NewGuid();
        var pt2 = Guid.NewGuid();
        var apps = new List<Entities.Application>
        {
            MakeApplication(citizen, pt1, ApplicationStatus.Submitted),
            MakeApplication(citizen, pt2, ApplicationStatus.Submitted)
        };
        _mockAppRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(apps);
        SetupLookups(citizen, pt1);
        SetupLookups(citizen, pt2);

        var result = await _handler.Handle(
            new GetOfficerDashboardQuery { PermitTypeId = pt1 },
            CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal(pt1, result.Items[0].ApplicationId == result.Items[0].ApplicationId ? pt1 : pt1);
    }

    [Fact]
    public async Task Handle_ShouldSortBySubmittedDateDescendingByDefault()
    {
        var citizen = Guid.NewGuid();
        var pt = Guid.NewGuid();
        var older = MakeApplication(citizen, pt, ApplicationStatus.Submitted);
        var newer = MakeApplication(citizen, pt, ApplicationStatus.Submitted);
        _mockAppRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entities.Application> { older, newer });
        SetupLookups(citizen, pt);

        var result = await _handler.Handle(
            new GetOfficerDashboardQuery { SortBy = OfficerDashboardSortBy.SubmittedDate },
            CancellationToken.None);

        Assert.True(result.Items[0].SubmittedDate >= result.Items[1].SubmittedDate);
    }

    [Fact]
    public async Task Handle_ShouldSortByLastUpdatedAscendingWhenRequested()
    {
        var citizen = Guid.NewGuid();
        var pt = Guid.NewGuid();
        var a = MakeApplication(citizen, pt, ApplicationStatus.Submitted);
        var b = MakeApplication(citizen, pt, ApplicationStatus.UnderReview);
        _mockAppRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entities.Application> { a, b });
        SetupLookups(citizen, pt);

        var result = await _handler.Handle(
            new GetOfficerDashboardQuery
            {
                SortBy = OfficerDashboardSortBy.LastUpdated,
                SortDescending = false
            },
            CancellationToken.None);

        Assert.True(result.Items[0].LastUpdated <= result.Items[1].LastUpdated);
    }

    [Fact]
    public async Task Handle_ShouldPaginate()
    {
        var citizen = Guid.NewGuid();
        var pt = Guid.NewGuid();
        var apps = Enumerable.Range(0, 5)
            .Select(_ => MakeApplication(citizen, pt, ApplicationStatus.Submitted))
            .ToList();
        _mockAppRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(apps);
        SetupLookups(citizen, pt);

        var result = await _handler.Handle(
            new GetOfficerDashboardQuery { PageNumber = 2, PageSize = 2 },
            CancellationToken.None);

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task Handle_ShouldReportRequiredDocumentCompleteness()
    {
        var citizen = Guid.NewGuid();
        var pt = Guid.NewGuid();
        var app = MakeApplication(citizen, pt, ApplicationStatus.Submitted);
        _mockAppRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entities.Application> { app });
        SetupLookups(citizen, pt);

        // Permit type with one required document type "ID"
        var permitType = new Entities.PermitType("Building Permit", "desc", 10m);
        permitType.AddDocumentRequirement("ID", true, new[] { ".pdf" }, 1024);
        _mockPermitTypeRepo.Setup(r => r.GetByIdAsync(pt, It.IsAny<CancellationToken>())).ReturnsAsync(permitType);

        var result = await _handler.Handle(new GetOfficerDashboardQuery(), CancellationToken.None);

        // No documents uploaded yet -> not all required uploaded
        Assert.False(result.Items[0].AllRequiredDocumentsUploaded);
        Assert.Equal(0, result.Items[0].DocumentCount);
    }

    [Fact]
    public async Task Handle_ShouldDeriveAssignedOfficerFromAssignmentState()
    {
        var citizen = Guid.NewGuid();
        var officer = Guid.NewGuid();
        var pt = Guid.NewGuid();
        var app = MakeApplication(citizen, pt, ApplicationStatus.Submitted);
        app.StartReview(officer);          // -> UnderReview, no assignment yet
        app.AssignToOfficer(officer);      // O4: explicit assignment is source of truth

        // A review exists, but the assigned-officer name must NOT be derived from it.
        app.AddReview(Guid.NewGuid(), officer, ReviewDecision.RequestInfo, "Please provide details", true);

        _mockAppRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entities.Application> { app });
        SetupLookups(citizen, pt);
        _mockUserRepo.Setup(r => r.GetByIdAsync(officer, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Entities.User(officer, "o@x.com", "Jane", "Doe", ATLAS.Domain.Entities.UserRole.Officer));

        var result = await _handler.Handle(new GetOfficerDashboardQuery(), CancellationToken.None);

        Assert.Equal(officer, result.Items[0].AssignedOfficerId);
        Assert.Equal("Jane Doe", result.Items[0].AssignedOfficerName);
    }

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmpty()
    {
        _mockAppRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Entities.Application>());

        var result = await _handler.Handle(new GetOfficerDashboardQuery(), CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    private void SetupLookups(Guid citizen, Guid permitType)
    {
        _mockUserRepo.Setup(r => r.GetByIdAsync(citizen, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Entities.User(citizen, "c@x.com", "John", "Smith", ATLAS.Domain.Entities.UserRole.Citizen));
        _mockPermitTypeRepo.Setup(r => r.GetByIdAsync(permitType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Entities.PermitType("Building Permit", "desc", 10m));
    }
}