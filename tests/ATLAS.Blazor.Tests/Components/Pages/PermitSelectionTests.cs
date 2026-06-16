using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.Components.Pages;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace ATLAS.Blazor.Tests.Components.Pages;

public class PermitSelectionTests : TestContext
{
    private readonly Mock<IMediator> _mediatorMock;

    public PermitSelectionTests()
    {
        _mediatorMock = new Mock<IMediator>();
        Services.AddSingleton(_mediatorMock.Object);
    }

    private static List<PermitTypeSummaryDto> CreateSamplePermitTypes()
    {
        return new List<PermitTypeSummaryDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Building Permit",
                Description = "For construction and renovation projects",
                Fee = 150.00m,
                Fields = new List<FieldDefinitionDto>()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Event Permit",
                Description = "For public events and gatherings",
                Fee = 0m,
                Fields = new List<FieldDefinitionDto>()
            }
        };
    }

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        // Arrange - use a TaskCompletionSource so the query stays pending
        var tcs = new TaskCompletionSource<IEnumerable<PermitTypeSummaryDto>>();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .Returns(tcs.Task);

        // Act
        var cut = Render<PermitSelection>();

        // Assert - component is still loading (task not completed)
        var spinner = cut.Find(".spinner-border");
        Assert.NotNull(spinner);
    }

    [Fact]
    public void Should_RenderPermitCards_WhenPermitTypesLoaded()
    {
        // Arrange
        var permitTypes = CreateSamplePermitTypes();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ReturnsAsync(permitTypes);

        // Act
        var cut = Render<PermitSelection>();

        // Assert
        var cards = cut.FindAll(".card");
        Assert.Equal(2, cards.Count);
        Assert.Contains("Building Permit", cards[0].TextContent);
        Assert.Contains("Event Permit", cards[1].TextContent);
    }

    [Fact]
    public void Should_ShowEmptyState_WhenNoPermitTypes()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ReturnsAsync(new List<PermitTypeSummaryDto>());

        // Act
        var cut = Render<PermitSelection>();

        // Assert
        var alert = cut.Find(".alert-info");
        Assert.NotNull(alert);
        Assert.Contains("No permit types available", alert.TextContent);
    }

    [Fact]
    public void Should_ShowErrorState_WhenQueryFails()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ThrowsAsync(new InvalidOperationException("Database connection failed"));

        // Act
        var cut = Render<PermitSelection>();

        // Assert
        var alert = cut.Find(".alert-danger");
        Assert.NotNull(alert);
        Assert.Contains("Something went wrong", alert.TextContent);
    }

    [Fact]
    public void Should_RenderApplyButton_OnEachCard()
    {
        // Arrange
        var permitTypes = CreateSamplePermitTypes();
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ReturnsAsync(permitTypes);

        // Act
        var cut = Render<PermitSelection>();

        // Assert
        var applyButtons = cut.FindAll("a.btn-primary");
        Assert.Equal(2, applyButtons.Count);
        Assert.All(applyButtons, btn => Assert.Contains("Apply", btn.TextContent));
    }

    [Fact]
    public void Should_GenerateCorrectNavigationUrl()
    {
        // Arrange
        var permitTypes = CreateSamplePermitTypes();
        var firstId = permitTypes[0].Id;
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ReturnsAsync(permitTypes);

        // Act
        var cut = Render<PermitSelection>();

        // Assert
        var firstCardLink = cut.FindAll("a.btn-primary")[0];
        Assert.EndsWith($"/applications/create/{firstId}", firstCardLink.GetAttribute("href"));
    }

    [Fact]
    public void Should_ShowFee_WhenPermitTypeHasFee()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ReturnsAsync(CreateSamplePermitTypes());

        // Act
        var cut = Render<PermitSelection>();

        // Assert - check the decimal value is rendered; "C" format symbol varies by culture
        var cards = cut.FindAll(".card");
        Assert.Contains("150", cards[0].TextContent);
    }

    [Fact]
    public void Should_NotShowFee_WhenPermitTypeHasNoFee()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ReturnsAsync(CreateSamplePermitTypes());

        // Act
        var cut = Render<PermitSelection>();

        // Assert
        var cards = cut.FindAll(".card");
        Assert.DoesNotContain("Fee:", cards[1].TextContent);
    }

    [Fact]
    public void Should_Retry_WhenErrorStateAndTryAgainClicked()
    {
        // Arrange
        _mediatorMock
            .SetupSequence(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ThrowsAsync(new InvalidOperationException("Fail"))
            .ReturnsAsync(CreateSamplePermitTypes());

        var cut = Render<PermitSelection>();

        // Act - wait for error state, then click Try Again
        var tryAgainButton = cut.Find("button.btn-outline-danger");
        tryAgainButton.Click();

        // Assert
        var cards = cut.FindAll(".card");
        Assert.Equal(2, cards.Count);
        Assert.Contains("Building Permit", cards[0].TextContent);
    }

    [Fact]
    public void Should_UseAccessibleButtonLabels()
    {
        // Arrange
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetActivePermitTypesQuery>(), default))
            .ReturnsAsync(CreateSamplePermitTypes());

        // Act
        var cut = Render<PermitSelection>();

        // Assert
        var buttons = cut.FindAll("a.btn-primary");
        Assert.All(buttons, btn =>
        {
            var ariaLabel = btn.GetAttribute("aria-label");
            Assert.NotNull(ariaLabel);
            Assert.StartsWith("Apply for", ariaLabel);
        });
    }
}