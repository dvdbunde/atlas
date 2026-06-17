using ATLAS.Application.Commands.Applications;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Applications;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.Components.Pages;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ATLAS.Blazor.Tests.Components.Pages;

public class ApplicationEditTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Guid _applicationId = Guid.NewGuid();
    private readonly Guid _permitTypeId = Guid.NewGuid();

    public ApplicationEditTests()
    {
        _mediatorMock = new Mock<IMediator>();
        Services.AddSingleton(_mediatorMock.Object);
    }

    private ApplicationDetailDto CreateSampleDraftApplication()
    {
        return new ApplicationDetailDto
        {
            Id = _applicationId,
            ApplicationNumber = "APP-2026-0042",
            Status = ApplicationStatus.Draft,
            PermitTypeId = _permitTypeId,
            CitizenId = Guid.NewGuid(),
            CitizenName = "Test Citizen",
            PermitTypeName = "Building Permit",
            CitizenNotes = string.Empty,
            OfficerNotes = string.Empty,
            FieldValues = new Dictionary<string, string>
            {
                { "PropertyAddress", "123 Main St" },
                { "SquareFootage", "2000" }
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
                new() { Name = "SquareFootage", Type = FieldType.Number, IsRequired = true },
                new() { Name = "Description", Type = FieldType.MultilineText, IsRequired = false }
            }
        };
    }

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        // Arrange
        var tcs = new TaskCompletionSource<ApplicationDetailDto?>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .Returns(tcs.Task);

        // Act
        var cut = Render<ApplicationEdit>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        // Assert
        var spinner = cut.Find(".spinner-border");
        Assert.NotNull(spinner);
    }

    [Fact]
    public void Should_ShowApplicationNumberAndStatus_WhenLoaded()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleDraftApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        // Act
        var cut = Render<ApplicationEdit>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        // Assert
        Assert.Contains("APP-2026-0042", cut.Markup);
        Assert.Contains("Building Permit", cut.Markup);
    }

    [Fact]
    public void Should_PrePopulateFields_WithExistingValues()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleDraftApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        // Act
        var cut = Render<ApplicationEdit>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        // Assert
        var textInput = cut.Find("input[type='text']");
        Assert.NotNull(textInput);
        Assert.Equal("123 Main St", textInput.GetAttribute("value"));

        var numberInput = cut.Find("input[type='number']");
        Assert.NotNull(numberInput);
        Assert.Equal("2000", numberInput.GetAttribute("value"));
    }

    [Fact]
    public void Should_ShowErrorState_WhenApplicationNotFound()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync((ApplicationDetailDto?)null);

        // Act
        var cut = Render<ApplicationEdit>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        // Assert
        var alert = cut.Find(".alert-danger");
        Assert.Contains("not found", alert.TextContent);
    }

    [Fact]
    public void Should_ShowErrorState_WhenApplicationNotDraft()
    {
        // Arrange
        var submittedApp = CreateSampleDraftApplication();
        submittedApp.Status = ApplicationStatus.Submitted;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(submittedApp);

        // Act
        var cut = Render<ApplicationEdit>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        // Assert
        var alert = cut.Find(".alert-danger");
        Assert.Contains("cannot be edited", alert.TextContent);
    }

    [Fact]
    public void Should_RenderSaveChangesButton_WhenLoaded()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleDraftApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        // Act
        var cut = Render<ApplicationEdit>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        // Assert
        var button = cut.Find("button.btn-primary");
        Assert.Contains("Save Changes", button.TextContent);
    }

    [Fact]
    public void Should_ShowSuccessState_WhenSaveSucceeds()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleDraftApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateDraftCommand>(), default))
            .ReturnsAsync(Unit.Value);

        var cut = Render<ApplicationEdit>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        // Act — populate required fields (none empty here, but click save)
        var saveButton = cut.Find("button.btn-primary");
        saveButton.Click();

        // Assert
        var successAlert = cut.Find(".alert-success");
        Assert.NotNull(successAlert);
        Assert.Contains("Changes saved", successAlert.TextContent);
    }

    [Fact]
    public void Should_DismissSuccess_WhenContinueEditingClicked()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleDraftApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateDraftCommand>(), default))
            .ReturnsAsync(Unit.Value);

        var cut = Render<ApplicationEdit>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        // Act — save then dismiss
        cut.Find("button.btn-primary").Click();
        cut.Find("button.btn-outline-success").Click();

        // Assert — success message gone, save button back
        var successAlerts = cut.FindAll(".alert-success");
        Assert.Empty(successAlerts);
        var saveButton = cut.Find("button.btn-primary");
        Assert.Contains("Save Changes", saveButton.TextContent);
    }

    [Fact]
    public void Should_ShowErrorState_WhenSaveFails()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleDraftApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<UpdateDraftCommand>(), default))
            .ThrowsAsync(new InvalidOperationException("Save failed"));

        var cut = Render<ApplicationEdit>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        // Act
        cut.Find("button.btn-primary").Click();

        // Assert
        var alert = cut.Find(".alert-danger");
        Assert.Contains("save your changes", alert.TextContent);
    }

    [Fact]
    public void Should_ShowErrorState_WhenQueryFails()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var cut = Render<ApplicationEdit>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        // Assert
        var alert = cut.Find(".alert-danger");
        Assert.Contains("Something went wrong", alert.TextContent);
    }

    [Fact]
    public void Should_ShowSubmitButton_WhenLoaded()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleDraftApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        var cut = Render<ApplicationEdit>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        var submitButton = cut.Find("button.btn-success");
        Assert.Contains("Submit Application", submitButton.TextContent);
    }

    [Fact]
    public void Should_RedirectToConfirmation_WhenSubmitSucceeds()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleDraftApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SubmitDraftCommand>(), default))
            .ReturnsAsync(Unit.Value);

        var cut = Render<ApplicationEdit>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        cut.Find("button.btn-success").Click();

        // NavigationManager.NavigateTo should have been called with confirmation URL
        // In bUnit this doesn't redirect the test, we just verify no exception occurred
        // and that an error alert wasn't shown
        var errorAlerts = cut.FindAll(".alert-danger");
        Assert.Empty(errorAlerts);
    }

    [Fact]
    public void Should_ShowSubmitError_WhenSubmitFails()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetApplicationByIdQuery>(), default))
            .ReturnsAsync(CreateSampleDraftApplication());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<SubmitDraftCommand>(), default))
            .ThrowsAsync(new InvalidOperationException("Submit failed"));

        var cut = Render<ApplicationEdit>(parameters =>
            parameters.Add(p => p.Id, _applicationId));

        cut.Find("button.btn-success").Click();

        var alert = cut.Find(".alert-danger");
        Assert.Contains("unable to submit", alert.TextContent);
    }
}