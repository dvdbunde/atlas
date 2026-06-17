using ATLAS.Application.Commands.Applications;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.Components.Pages;
using ATLAS.Blazor.ViewModels;
using ATLAS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ATLAS.Blazor.Tests.Components.Pages;

public class ApplicationCreateTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Guid _permitTypeId = Guid.NewGuid();

    public ApplicationCreateTests()
    {
        _mediatorMock = new Mock<IMediator>();
        Services.AddSingleton(_mediatorMock.Object);
    }

    private PermitTypeDto CreateSamplePermitType()
    {
        return new PermitTypeDto
        {
            Id = _permitTypeId,
            Name = "Building Permit",
            Description = "For construction and renovation projects",
            Fee = 150.00m,
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
        var tcs = new TaskCompletionSource<PermitTypeDto?>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .Returns(tcs.Task);

        // Act
        var cut = Render<ApplicationCreate>(parameters =>
            parameters.Add(p => p.PermitTypeId, _permitTypeId));

        // Assert
        var spinner = cut.Find(".spinner-border");
        Assert.NotNull(spinner);
    }

    [Fact]
    public void Should_ShowPermitNameAndDescription_WhenLoaded()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        // Act
        var cut = Render<ApplicationCreate>(parameters =>
            parameters.Add(p => p.PermitTypeId, _permitTypeId));

        // Assert
        Assert.Contains("Building Permit", cut.Markup);
        Assert.Contains("For construction and renovation projects", cut.Markup);
    }

    [Fact]
    public void Should_RenderDynamicFormGenerator_WhenPermitTypeLoaded()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        // Act
        var cut = Render<ApplicationCreate>(parameters =>
            parameters.Add(p => p.PermitTypeId, _permitTypeId));

        // Assert - DynamicFormGenerator renders input for each field
        var textInput = cut.Find("input[type='text']");
        Assert.NotNull(textInput);

        var numberInput = cut.Find("input[type='number']");
        Assert.NotNull(numberInput);

        var textarea = cut.Find("textarea");
        Assert.NotNull(textarea);
    }

    [Fact]
    public void Should_RenderSaveDraftButton_WhenPermitTypeLoaded()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        // Act
        var cut = Render<ApplicationCreate>(parameters =>
            parameters.Add(p => p.PermitTypeId, _permitTypeId));

        // Assert
        var button = cut.Find("button.btn-primary");
        Assert.Contains("Save Draft", button.TextContent);
    }

    [Fact]
    public void Should_ShowErrorState_WhenPermitTypeNotFound()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync((PermitTypeDto?)null);

        // Act
        var cut = Render<ApplicationCreate>(parameters =>
            parameters.Add(p => p.PermitTypeId, _permitTypeId));

        // Assert
        var alert = cut.Find(".alert-danger");
        Assert.NotNull(alert);
        Assert.Contains("not found", alert.TextContent);
    }

    [Fact]
    public void Should_ShowErrorState_WhenQueryFails()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var cut = Render<ApplicationCreate>(parameters =>
            parameters.Add(p => p.PermitTypeId, _permitTypeId));

        // Assert
        var alert = cut.Find(".alert-danger");
        Assert.NotNull(alert);
        Assert.Contains("Something went wrong", alert.TextContent);
    }

    [Fact]
    public void Should_ShowSuccessState_WhenDraftSaved()
    {
        // Arrange
        var appId = Guid.NewGuid();
        _mediatorMock
            .SetupSequence(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateDraftCommand>(), default))
            .ReturnsAsync(appId);

        var cut = Render<ApplicationCreate>(parameters =>
            parameters.Add(p => p.PermitTypeId, _permitTypeId));

        // Act - populate required fields first, then save
        cut.Find("input[type='text']").Change("123 Main St");
        cut.Find("input[type='number']").Change("2000");
        var saveButton = cut.Find("button.btn-primary");
        saveButton.Click();

        // Assert
        var successAlert = cut.Find(".alert-success");
        Assert.NotNull(successAlert);
        Assert.Contains("Draft saved", successAlert.TextContent);
    }

    [Fact]
    public void Should_ShowContinueEditingLink_AfterDraftSaved()
    {
        // Arrange
        var appId = Guid.NewGuid();
        _mediatorMock
            .SetupSequence(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateDraftCommand>(), default))
            .ReturnsAsync(appId);

        var cut = Render<ApplicationCreate>(parameters =>
            parameters.Add(p => p.PermitTypeId, _permitTypeId));

        // Act - populate required fields, then save
        cut.Find("input[type='text']").Change("123 Main St");
        cut.Find("input[type='number']").Change("2000");
        var saveButton = cut.Find("button.btn-primary");
        saveButton.Click();

        // Assert
        var editLink = cut.Find("a.btn-primary");
        Assert.EndsWith($"/applications/edit/{appId}", editLink.GetAttribute("href"));
    }

    [Fact]
    public void Should_DisableSaveButton_WhileSaving()
    {
        // Arrange - use a TaskCompletionSource so save stays pending
        var loadTcs = new TaskCompletionSource<PermitTypeDto?>();
        _mediatorMock
            .SetupSequence(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        var saveTcs = new TaskCompletionSource<Guid>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateDraftCommand>(), default))
            .Returns(saveTcs.Task);

        var cut = Render<ApplicationCreate>(parameters =>
            parameters.Add(p => p.PermitTypeId, _permitTypeId));

        // Act - populate required fields, then save
        cut.Find("input[type='text']").Change("123 Main St");
        cut.Find("input[type='number']").Change("2000");
        var saveButton = cut.Find("button.btn-primary");
        saveButton.Click();

        // Assert - button should show "Saving..." and be disabled
        var disabledButton = cut.Find("button[disabled]");
        Assert.NotNull(disabledButton);
        Assert.Contains("Saving...", disabledButton.TextContent);
    }

    [Fact]
    public void Should_ShowErrorState_WhenSaveFails()
    {
        // Arrange
        _mediatorMock
            .SetupSequence(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<CreateDraftCommand>(), default))
            .ThrowsAsync(new InvalidOperationException("Save failed"));

        var cut = Render<ApplicationCreate>(parameters =>
            parameters.Add(p => p.PermitTypeId, _permitTypeId));

        // Act - populate required fields, then save
        cut.Find("input[type='text']").Change("123 Main St");
        cut.Find("input[type='number']").Change("2000");
        var saveButton = cut.Find("button.btn-primary");
        saveButton.Click();

        // Assert
        var alert = cut.Find(".alert-danger");
        Assert.NotNull(alert);
        Assert.Contains("save your draft", alert.TextContent);
    }

    [Fact]
    public void Should_ShowRequiredFieldValidation_WhenSaveClicked_WithEmptyRequiredFields()
    {
        // Arrange
        _mediatorMock
            .SetupSequence(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(CreateSamplePermitType());

        var cut = Render<ApplicationCreate>(parameters =>
            parameters.Add(p => p.PermitTypeId, _permitTypeId));

        // Act - click save without filling required fields
        var saveButton = cut.Find("button.btn-primary");
        saveButton.Click();

        // Assert
        var validationMessages = cut.FindAll(".invalid-feedback");
        Assert.NotEmpty(validationMessages);
        Assert.Contains(validationMessages, msg =>
            msg.TextContent.Contains("PropertyAddress is required"));
    }
}