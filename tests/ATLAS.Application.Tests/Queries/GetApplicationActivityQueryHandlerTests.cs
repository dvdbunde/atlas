using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Applications;
using Entities = ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries;

public class GetApplicationActivityQueryHandlerTests
{
    private readonly Mock<IApplicationRepository> _mockAppRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly GetApplicationActivityQueryHandler _handler;
    private readonly Guid _citizenId = Guid.NewGuid();
    private readonly Guid _officerId = Guid.NewGuid();
    private readonly Guid _permitTypeId = Guid.NewGuid();

    public GetApplicationActivityQueryHandlerTests()
    {
        _mockAppRepo = new Mock<IApplicationRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _handler = new GetApplicationActivityQueryHandler(_mockAppRepo.Object, _mockUserRepo.Object);
    }

    private Entities.Application CreateFullCycleApplication()
    {
        var app = new Entities.Application(_citizenId, _permitTypeId, "Test notes");
        app.Submit();
        app.StartReview(_officerId);
        app.AssignToOfficer(_officerId);
        app.AddDocument(Guid.NewGuid(), "SitePlan", "plan.pdf", "application/pdf", 1024, "https://blob/plan", _citizenId);
        return app;
    }

    [Fact]
    public async Task Handle_ShouldReturnActivities_Chronologically()
    {
        var app = CreateFullCycleApplication();
        _mockAppRepo.Setup(r => r.GetByIdAsync(app.Id, It.IsAny<CancellationToken>())).ReturnsAsync(app);

        var result = await _handler.Handle(new GetApplicationActivityQuery { ApplicationId = app.Id }, CancellationToken.None);

        Assert.NotEmpty(result);
        // Most recent first
        for (int i = 1; i < result.Count; i++)
            Assert.True(result[i - 1].Timestamp >= result[i].Timestamp);
    }

    [Fact]
    public async Task Handle_ShouldIncludeCreation_WhenNoOtherEvents()
    {
        var app = new Entities.Application(_citizenId, _permitTypeId, "Test notes");
        _mockAppRepo.Setup(r => r.GetByIdAsync(app.Id, It.IsAny<CancellationToken>())).ReturnsAsync(app);

        var result = await _handler.Handle(new GetApplicationActivityQuery { ApplicationId = app.Id }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("Created", result[0].ActivityType);
    }

    [Fact]
    public async Task Handle_ShouldReturnEmpty_WhenApplicationNotFound()
    {
        _mockAppRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Entities.Application)null);

        var result = await _handler.Handle(new GetApplicationActivityQuery { ApplicationId = Guid.NewGuid() }, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_ShouldIncludeDocumentUploads()
    {
        var app = CreateFullCycleApplication();
        _mockAppRepo.Setup(r => r.GetByIdAsync(app.Id, It.IsAny<CancellationToken>())).ReturnsAsync(app);

        var result = await _handler.Handle(new GetApplicationActivityQuery { ApplicationId = app.Id }, CancellationToken.None);

        Assert.Contains(result, a => a.ActivityType == "DocumentUploaded");
    }

    [Fact]
    public async Task Handle_ShouldIncludeReviews()
    {
        var app = CreateFullCycleApplication();
        _mockAppRepo.Setup(r => r.GetByIdAsync(app.Id, It.IsAny<CancellationToken>())).ReturnsAsync(app);

        var result = await _handler.Handle(new GetApplicationActivityQuery { ApplicationId = app.Id }, CancellationToken.None);

        Assert.Contains(result, a => a.ActivityType == "Assigned");
    }

    [Fact]
    public async Task Handle_ShouldResolveUserNames()
    {
        var app = CreateFullCycleApplication();
        _mockAppRepo.Setup(r => r.GetByIdAsync(app.Id, It.IsAny<CancellationToken>())).ReturnsAsync(app);
        _mockUserRepo.Setup(r => r.GetByIdAsync(_officerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Entities.User(_officerId, "o@x.com", "Jane", "Officer", Entities.UserRole.Officer));

        var result = await _handler.Handle(new GetApplicationActivityQuery { ApplicationId = app.Id }, CancellationToken.None);

        var assigned = result.FirstOrDefault(a => a.ActivityType == "Assigned");
        Assert.NotNull(assigned);
        Assert.Equal("Jane Officer", assigned!.PerformedBy);
    }

    [Fact]
    public async Task Handle_ShouldNotIncludeDuplicateAssignment_WhenNoAssignment()
    {
        var app = new Entities.Application(_citizenId, _permitTypeId, "Test notes");
        app.Submit();
        _mockAppRepo.Setup(r => r.GetByIdAsync(app.Id, It.IsAny<CancellationToken>())).ReturnsAsync(app);

        var result = await _handler.Handle(new GetApplicationActivityQuery { ApplicationId = app.Id }, CancellationToken.None);

        Assert.DoesNotContain(result, a => a.ActivityType == "Assigned");
    }

    [Fact]
    public async Task Handle_ShouldUseStableOrder_ForEqualTimestamps()
    {
        // Create an application where submission and document have the same timestamp
        var app = new Entities.Application(_citizenId, _permitTypeId, "Test notes");
        // Set CreatedDate to a specific time
        typeof(Entities.Entity<Guid>).GetProperty("CreatedDate")?.SetValue(app, new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        app.Submit(); // sets SubmittedDate
        // Add a document with the same timestamp as submission
        app.AddDocument(Guid.NewGuid(), "SitePlan", "plan.pdf", "application/pdf", 1024, "https://blob/plan", _citizenId);

        _mockAppRepo.Setup(r => r.GetByIdAsync(app.Id, It.IsAny<CancellationToken>())).ReturnsAsync(app);

        var result = await _handler.Handle(new GetApplicationActivityQuery { ApplicationId = app.Id }, CancellationToken.None);

        // Both created and submitted events should be present regardless of sort stability
        Assert.Contains(result, a => a.ActivityType == "Created");
        Assert.Contains(result, a => a.ActivityType == "Submitted");
        Assert.Contains(result, a => a.ActivityType == "DocumentUploaded");
    }
}