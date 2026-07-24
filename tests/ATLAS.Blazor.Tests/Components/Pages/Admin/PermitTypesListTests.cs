using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.Components.Pages.Admin;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

public class PermitTypesListTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();

    public PermitTypesListTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
    }

    private static List<PermitTypeSummaryDto> SamplePermitTypes() => new()
    {
        new PermitTypeSummaryDto { Id = Guid.NewGuid(), Name = "Building Permit", Description = "Desc", Fee = 100m, IsActive = true, FieldCount = 2, DocumentRequirementCount = 1 },
        new PermitTypeSummaryDto { Id = Guid.NewGuid(), Name = "Event Permit", Description = "Desc", Fee = 50m, IsActive = false, FieldCount = 0, DocumentRequirementCount = 0 }
    };

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var tcs = new TaskCompletionSource<IEnumerable<PermitTypeSummaryDto>>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypesQuery>(), default)).Returns(tcs.Task);

        var cut = Render<PermitTypes>();

        Assert.NotNull(cut.Find(".spinner-border"));
    }

    [Fact]
    public void Should_RenderTableRows_WhenLoaded()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypesQuery>(), default))
            .ReturnsAsync(SamplePermitTypes());

        var cut = Render<PermitTypes>();

        Assert.Equal(2, cut.FindAll("tbody tr").Count);
        Assert.Contains("Building Permit", cut.Markup);
    }

    [Fact]
    public void Should_ShowErrorState_WhenQueryFails()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypesQuery>(), default))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var cut = Render<PermitTypes>();

        Assert.NotNull(cut.Find(".alert-danger"));
    }

    [Fact]
    public void Should_ShowEmptyState_WhenNoResults()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypesQuery>(), default))
            .ReturnsAsync(new List<PermitTypeSummaryDto>());

        var cut = Render<PermitTypes>();

        Assert.NotNull(cut.Find(".alert-info"));
    }

    [Fact]
    public void Should_PassSearchTerm_ToQuery()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypesQuery>(), default))
            .ReturnsAsync(SamplePermitTypes());

        var cut = Render<PermitTypes>();

        var input = cut.Find("#pt-search");
        input.Input("building");

        _mediatorMock.Verify(m => m.Send(It.Is<GetPermitTypesQuery>(q => q.SearchTerm == "building"), default), Times.AtLeastOnce);
    }
}
