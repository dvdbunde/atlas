using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.Components.Pages.Admin;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

public class PermitTypeDetailTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();

    public PermitTypeDetailTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
    }

    private static PermitTypeDto SampleDto(Guid id) => new()
    {
        Id = id,
        Name = "Building Permit",
        Description = "Construction permit",
        Fee = 100m,
        IsActive = true,
        Fields = new List<FieldDefinitionDto>
        {
            new() { Name = "ApplicantName", Type = ATLAS.Domain.Enums.FieldType.Text, IsRequired = true }
        },
        DocumentRequirements = new List<FieldDefinitionDto>
        {
            new() { Name = "Passport", Type = ATLAS.Domain.Enums.FieldType.FileUpload, IsRequired = true }
        }
    };

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var id = Guid.NewGuid();
        var tcs = new TaskCompletionSource<PermitTypeDto?>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default)).Returns(tcs.Task);

        var cut = Render<PermitTypeDetail>(parameters => parameters.Add(p => p.Id, id.ToString()));

        Assert.NotNull(cut.Find(".spinner-border"));
    }

    [Fact]
    public void Should_RenderDetails_WhenLoaded()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));

        var cut = Render<PermitTypeDetail>(parameters => parameters.Add(p => p.Id, id.ToString()));

        Assert.Contains("Building Permit", cut.Markup);
        Assert.Contains("Configured Fields", cut.Markup);
        Assert.Contains("Document Requirements", cut.Markup);
        Assert.Contains("ApplicantName", cut.Markup);
        Assert.Contains("Passport", cut.Markup);
    }

    [Fact]
    public void Should_ShowNotFound_WhenDtoIsNull()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync((PermitTypeDto?)null);

        var cut = Render<PermitTypeDetail>(parameters => parameters.Add(p => p.Id, id.ToString()));

        Assert.NotNull(cut.Find(".alert-warning"));
    }

    [Fact]
    public void Should_ShowNotFound_WhenIdIsInvalid()
    {
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(Guid.NewGuid()));

        var cut = Render<PermitTypeDetail>(parameters => parameters.Add(p => p.Id, "not-a-guid"));

        Assert.NotNull(cut.Find(".alert-warning"));
    }

    [Fact]
    public void Should_ShowErrorState_WhenQueryFails()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var cut = Render<PermitTypeDetail>(parameters => parameters.Add(p => p.Id, id.ToString()));

        Assert.NotNull(cut.Find(".alert-danger"));
    }
}
