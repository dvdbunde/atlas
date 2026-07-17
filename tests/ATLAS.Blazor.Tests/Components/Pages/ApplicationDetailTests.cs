using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Applications;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.Components.Pages;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ATLAS.Blazor.Tests.Components.Pages;

public class ApplicationDetailTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _permitTypeId = Guid.NewGuid();

    public ApplicationDetailTests()
    {
        _mediatorMock = new Mock<IMediator>();
        Services.AddSingleton(_mediatorMock.Object);
    }

    private ApplicationDetailDto CreateSampleSubmittedApplication()
    {
        return new ApplicationDetailDto
        {
            Id = _applicationId,
            ApplicationNumber = "APP-2026-0042",
            Status = ApplicationStatus.Submitted,
            PermitTypeId = _permitTypeId,
            CitizenId = Guid.NewGuid(),
            CitizenName = "Test Citizen",
            PermitTypeName = "Building Permit",
            CitizenNotes = "Building a new garage",
            SubmittedDate = DateTime.UtcNow.AddDays(-2),
            FieldValues = new Dictionary<string, string>
            {
                { "PropertyAddress", "123 Main St" },
                { "SquareFootage", "2000" }
            },
            Reviews = new List<ReviewDto>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    OfficerId = Guid.NewGuid(),
                    Decision = ReviewDecision.RequestInfo,
                    Comments = "Please provide survey documents.",
                    ReviewedDate = DateTime.UtcNow.AddDays(-1),
                    IsVisibleToCitizen = true
                }
            }
        };
    }

    private PermitTypeDto CreateSamplePermitType()
    {
        return new PermitTypeDto
        {
            Id = _permitTypeId,
            Name = "Building Permit",
            Description = "For construction and renovation projects",
            Fee = 150.00m,
            IsActive = true,
            Fields = new List<FieldDefinitionDto>
            {
                new() { Name = "PropertyAddress", Type = FieldType.Text, IsRequired = true },
                new() { Name = "SquareFootage", Type = FieldType.Number, IsRequired = true }
            }
        };
    }

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var tcs = new TaskCompletionSource<ApplicationDetailDto?>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .Returns(tcs.Task);

        var cut = Render<ApplicationDetail>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        var spinner = cut.Find(".spinner-border");
        Assert.NotNull(spinner);
    }

    [Fact]
    public void Should_ShowApplicationNumber_WhenLoaded()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleSubmittedApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        var cut = Render<ApplicationDetail>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        Assert.Contains("APP-2026-0042", cut.Markup);
    }

    [Fact]
    public void Should_ShowStatusBadge_WhenLoaded()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleSubmittedApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        var cut = Render<ApplicationDetail>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        var badge = cut.Find(".badge");
        Assert.NotNull(badge);
        Assert.Contains("Submitted", badge.TextContent);
    }

    [Fact]
    public void Should_ShowFieldValues_ReadOnly()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleSubmittedApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        var cut = Render<ApplicationDetail>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        Assert.Contains("123 Main St", cut.Markup);
        Assert.Contains("2000", cut.Markup);
    }

    [Fact]
    public void Should_ShowActivityFeed_WhenLoaded()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleSubmittedApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationActivityQuery>(), default))
            .ReturnsAsync(new List<ApplicationActivityDto>
            {
                new()
                {
                    Timestamp = DateTime.UtcNow.AddDays(-1),
                    ActivityType = "Approved",
                    Title = "Application Approved",
                    Description = "Application was approved",
                    PerformedBy = "Jane Officer",
                    PerformedByRole = "Officer"
                }
            });

        var cut = Render<ApplicationDetail>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        var activityFeed = cut.Find(".application-activity-feed");
        Assert.NotNull(activityFeed);
    }

    [Fact]
    public void Should_ShowCitizenNotes_WhenProvided()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleSubmittedApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        var cut = Render<ApplicationDetail>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        Assert.Contains("Building a new garage", cut.Markup);
    }

    [Fact]
    public void Should_ShowReviews_WhenVisibleToCitizen()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleSubmittedApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        var cut = Render<ApplicationDetail>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        Assert.Contains("Please provide survey documents", cut.Markup);
    }

    [Fact]
    public void Should_ShowErrorState_WhenApplicationNotFound()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync((ApplicationDetailDto?)null);

        var cut = Render<ApplicationDetail>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        var alert = cut.Find(".alert-danger");
        Assert.Contains("not found", alert.TextContent);
    }

    [Fact]
    public void Should_ShowErrorState_WhenQueryFails()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        var cut = Render<ApplicationDetail>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        var alert = cut.Find(".alert-danger");
        Assert.Contains("Something went wrong", alert.TextContent);
    }

    [Fact]
    public void Should_ShowActivityEntries_WhenLoaded()
    {
        // Arrange
        var activities = new List<ApplicationActivityDto>
        {
            new()
            {
                Timestamp = DateTime.UtcNow.AddDays(-1),
                ActivityType = "Approved",
                Title = "Application Approved",
                Description = "Application was approved",
                PerformedBy = "Jane Officer",
                PerformedByRole = "Officer"
            },
            new()
            {
                Timestamp = DateTime.UtcNow.AddDays(-2),
                ActivityType = "Submitted",
                Title = "Application Submitted",
                Description = "Application was submitted for review"
            }
        };
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleSubmittedApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationActivityQuery>(), default))
            .ReturnsAsync(activities);

        // Act
        var cut = Render<ApplicationDetail>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        // Assert
        Assert.Contains("Application Approved", cut.Markup);
        Assert.Contains("Application Submitted", cut.Markup);
        Assert.Contains("Jane Officer", cut.Markup);
        Assert.Contains("Officer", cut.Markup);
    }

    [Fact]
    public void Should_ShowNoActivityState_WhenEmpty()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleSubmittedApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationActivityQuery>(), default))
            .ReturnsAsync(new List<ApplicationActivityDto>());

        // Act
        var cut = Render<ApplicationDetail>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        // Assert
        Assert.Contains("Loading activity...", cut.Markup);
    }
}