using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Applications;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.Components.Pages;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ATLAS.Blazor.Tests.Components.Pages;

public class ConfirmationPageTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _permitTypeId = Guid.NewGuid();

    public ConfirmationPageTests()
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
            PermitTypeName = "Building Permit",
            SubmittedDate = DateTime.UtcNow,
            CitizenNotes = string.Empty,
            FieldValues = new Dictionary<string, string>()
        };
    }

    private PermitTypeDto CreateSamplePermitType()
    {
        return new PermitTypeDto
        {
            Id = _permitTypeId,
            Name = "Building Permit",
            Description = "For construction projects",
            Fee = 150.00m,
            IsActive = true,
            Fields = new List<FieldDefinitionDto>()
        };
    }

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var tcs = new TaskCompletionSource<ApplicationDetailDto?>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .Returns(tcs.Task);

        var cut = Render<ConfirmationPage>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        var spinner = cut.Find(".spinner-border");
        Assert.NotNull(spinner);
    }

    [Fact]
    public void Should_ShowSuccessMessage_WhenLoaded()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleSubmittedApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        var cut = Render<ConfirmationPage>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        Assert.Contains("Submitted Successfully", cut.Markup);
        Assert.Contains("APP-2026-0042", cut.Markup);
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

        var cut = Render<ConfirmationPage>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        Assert.Contains("APP-2026-0042", cut.Markup);
    }

    [Fact]
    public void Should_ShowViewApplicationLink_WhenLoaded()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleSubmittedApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        var cut = Render<ConfirmationPage>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        var viewLink = cut.Find("a[href='/applications/" + _applicationId + "']");
        Assert.NotNull(viewLink);
        Assert.Contains("View Application", viewLink.TextContent);
    }

    [Fact]
    public void Should_ShowDashboardLink_WhenLoaded()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleSubmittedApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        var cut = Render<ConfirmationPage>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        var dashboardLink = cut.Find("a[href='/dashboard']");
        Assert.NotNull(dashboardLink);
        Assert.Contains("Go to Dashboard", dashboardLink.TextContent);
    }

    [Fact]
    public void Should_ShowErrorState_WhenApplicationNotFound()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync((ApplicationDetailDto?)null);

        var cut = Render<ConfirmationPage>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        var alert = cut.Find(".alert-danger");
        Assert.Contains("not found", alert.TextContent);
    }

    [Fact]
    public void Should_ShowDashboardLink_WhenError()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync((ApplicationDetailDto?)null);

        var cut = Render<ConfirmationPage>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        var dashboardLink = cut.Find("a[href='/dashboard']");
        Assert.NotNull(dashboardLink);
    }
}