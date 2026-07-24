using System.Security.Claims;
using ATLAS.Application.Commands.PermitTypes;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Blazor.Components.Pages.Admin;
using ATLAS.Blazor.Components.Shared;
using ATLAS.Blazor.FormModel;
using ATLAS.Domain.Enums;
using ATLAS.Blazor.Components.Shared.Admin;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

public class PermitTypeDesignerTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();

    public PermitTypeDesignerTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
        SetupAuth();
    }

    private static PermitTypeDto SampleDto(Guid id) => new()
    {
        Id = id,
        Name = "Building Permit",
        Description = "Construction permit",
        Fee = 100m,
        IsActive = true
    };

    private void SetupAuth()
    {
        var claims = new List<Claim> { new Claim("oid", Guid.NewGuid().ToString()) };
        var identity = new ClaimsIdentity(claims, "test");
        var user = new ClaimsPrincipal(identity);
        var provider = new TestAuthStateProvider(new AuthenticationState(user));
        Services.AddSingleton<AuthenticationStateProvider>(provider);
    }

    private sealed class TestAuthStateProvider : AuthenticationStateProvider
    {
        private readonly AuthenticationState _state;
        public TestAuthStateProvider(AuthenticationState state) => _state = state;
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(_state);
    }

    [Fact]
    public void Should_ShowLoadingIndicator_WhenPageLoads()
    {
        var id = Guid.NewGuid();
        var tcs = new TaskCompletionSource<PermitTypeDto?>();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default)).Returns(tcs.Task);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        Assert.NotNull(cut.Find(".spinner-border"));
    }

    [Fact]
    public void Should_RenderGeneralSectionByDefault_WhenLoaded()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        Assert.NotNull(cut.Find("#ptd-name"));
        Assert.NotNull(cut.Find("#ptd-description"));
        Assert.Contains("General", cut.Markup);
    }

    [Fact]
    public void Should_RenderPlaceholderSections_WhenNavigated()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        var fieldsTab = cut.FindAll("button").First(b => b.TextContent.Contains("Fields"));
        fieldsTab.Click();
        Assert.Contains("Permit Fields", cut.Markup);
        Assert.Contains("No fields configured yet", cut.Markup);

        var docsTab = cut.FindAll("button").First(b => b.TextContent.Contains("Document Requirements"));
        docsTab.Click();
        Assert.Contains("Document Requirements", cut.Markup);
        Assert.Contains("No document requirements configured yet", cut.Markup);

        var previewTab = cut.FindAll("button").First(b => b.TextContent.Contains("Preview"));
        previewTab.Click();
        Assert.Contains("Live Preview", cut.Markup);
    }

    [Fact]
    public void Should_ShowNotFound_WhenDtoIsNull()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync((PermitTypeDto?)null);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        Assert.Contains("Permit type not found", cut.Markup);
    }

    [Fact]
    public void Should_SendUpdateCommand_OnSave_WhenDirty()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdatePermitTypeGeneralInformationCommand>(), default))
            .ReturnsAsync(true);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        var nameInput = cut.Find("#ptd-name");
        nameInput.Input("Renovation Permit");

        Assert.Contains("You have unsaved changes", cut.Markup);

        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Save"));
        saveButton.Click();

        _mediatorMock.Verify(m => m.Send(It.Is<UpdatePermitTypeGeneralInformationCommand>(c =>
            c.PermitTypeId == id && c.Name == "Renovation Permit"), default), Times.Once);
    }

    [Fact]
    public void Should_NavigateToDetail_OnCancel_WhenDirty()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));
        var nav = Services.GetRequiredService<NavigationManager>();

        cut.Find("#ptd-name").Input("Renovation Permit");
        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("Cancel"));
        cancelButton.Click();

        Assert.EndsWith($"/admin/permit-types/{id}", nav.Uri);
    }

    [Fact]
    public void Should_RegisterUnsavedChanges_OnInput()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        cut.Find("#ptd-name").Input("Changed Name");

        Assert.Contains("You have unsaved changes", cut.Markup);
    }

    private static PermitTypeDto SampleDtoWithFields(Guid id)
    {
        var dto = SampleDto(id);
        dto.Fields = new List<FieldDefinitionDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Applicant Name", Type = FieldType.Text, IsRequired = true },
            new() { Id = Guid.NewGuid(), Name = "Category", Type = FieldType.Dropdown, IsRequired = false, Options = new List<string> { "A", "B" } }
        };
        return dto;
    }

    [Fact]
    public void Should_RenderFieldList_WhenFieldsTabSelected()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDtoWithFields(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        cut.FindAll("button").First(b => b.TextContent.Contains("Fields")).Click();

        Assert.Contains("Applicant Name", cut.Markup);
        Assert.Contains("Category", cut.Markup);
        Assert.Contains("Required", cut.Markup);
    }

    [Fact]
    public void Should_SendAddPermitFieldCommand_WhenAddingField()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));
        _mediatorMock.Setup(m => m.Send(It.IsAny<AddPermitFieldCommand>(), default)).ReturnsAsync(true);
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));
        cut.FindAll("button").First(b => b.TextContent.Contains("Fields")).Click();

        cut.FindAll("button").First(b => b.TextContent.Contains("Add Field")).Click();
        cut.Find("#ptd-field-name").Change("New Field");
        cut.FindAll("button").First(b => b.TextContent.Contains("Save")).Click();

        _mediatorMock.Verify(m => m.Send(It.Is<AddPermitFieldCommand>(c =>
            c.PermitTypeId == id && c.Name == "New Field" && c.Type == FieldType.Text), default), Times.Once);
    }

    [Fact]
    public void Should_SendUpdatePermitFieldCommand_WhenEditingField()
    {
        var id = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var dto = SampleDto(id);
        dto.Fields = new List<FieldDefinitionDto> { new() { Id = fieldId, Name = "Old", Type = FieldType.Text, IsRequired = false } };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default)).ReturnsAsync(dto);
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdatePermitFieldCommand>(), default)).ReturnsAsync(true);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));
        cut.FindAll("button").First(b => b.TextContent.Contains("Fields")).Click();

        cut.Find("button[title='Edit']").Click();
        cut.Find("#ptd-field-name").Change("Renamed");
        cut.FindAll("button").First(b => b.TextContent.Contains("Save")).Click();

        _mediatorMock.Verify(m => m.Send(It.Is<UpdatePermitFieldCommand>(c =>
            c.PermitTypeId == id && c.FieldId == fieldId && c.Name == "Renamed"), default), Times.Once);
    }

    [Fact]
    public void Should_SendRemovePermitFieldCommand_WhenRemovingField()
    {
        var id = Guid.NewGuid();
        var fieldId = Guid.NewGuid();
        var dto = SampleDto(id);
        dto.Fields = new List<FieldDefinitionDto> { new() { Id = fieldId, Name = "Temp", Type = FieldType.Text, IsRequired = false } };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default)).ReturnsAsync(dto);
        _mediatorMock.Setup(m => m.Send(It.IsAny<RemovePermitFieldCommand>(), default)).ReturnsAsync(true);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));
        cut.FindAll("button").First(b => b.TextContent.Contains("Fields")).Click();

        cut.Find("button[title='Remove']").Click();

        _mediatorMock.Verify(m => m.Send(It.Is<RemovePermitFieldCommand>(c =>
            c.PermitTypeId == id && c.FieldId == fieldId), default), Times.Once);
    }

    [Fact]
    public void Should_SendMovePermitFieldCommand_WhenReordering()
    {
        var id = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var dto = SampleDto(id);
        dto.Fields = new List<FieldDefinitionDto>
        {
            new() { Id = firstId, Name = "First", Type = FieldType.Text, IsRequired = false },
            new() { Id = secondId, Name = "Second", Type = FieldType.Text, IsRequired = false }
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default)).ReturnsAsync(dto);
        _mediatorMock.Setup(m => m.Send(It.IsAny<MovePermitFieldCommand>(), default)).ReturnsAsync(true);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));
        cut.FindAll("button").First(b => b.TextContent.Contains("Fields")).Click();

        // Move "First" down (direction +1) -> NewOrder 2
        var moveDownButtons = cut.FindAll("button[title='Move down']");
        moveDownButtons[0].Click();

        _mediatorMock.Verify(m => m.Send(It.Is<MovePermitFieldCommand>(c =>
            c.PermitTypeId == id && c.FieldId == firstId && c.NewOrder == 2), default), Times.Once);
    }

    private static PermitTypeDto SampleDtoWithRequirements(Guid id)
    {
        var dto = SampleDto(id);
        dto.DocumentRequirements = new List<FieldDefinitionDto>
        {
            new() { Id = Guid.NewGuid(), Name = "ID Copy", Type = FieldType.FileUpload, IsRequired = true, AllowedExtensions = ".pdf", MaxFileSizeBytes = 102400 },
            new() { Id = Guid.NewGuid(), Name = "Photo", Type = FieldType.FileUpload, IsRequired = false, AllowedExtensions = ".png", MaxFileSizeBytes = 204800 }
        };
        return dto;
    }

    [Fact]
    public void Should_RenderRequirementList_WhenDocumentsTabSelected()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDtoWithRequirements(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        cut.FindAll("button").First(b => b.TextContent.Contains("Document Requirements")).Click();

        Assert.Contains("ID Copy", cut.Markup);
        Assert.Contains("Photo", cut.Markup);
        Assert.Contains("Required", cut.Markup);
    }

    [Fact]
    public void Should_SendAddDocumentRequirementCommand_WhenAddingRequirement()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default)).ReturnsAsync(SampleDto(id));
        _mediatorMock.Setup(m => m.Send(It.IsAny<AddDocumentRequirementCommand>(), default)).ReturnsAsync(true);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));
        cut.FindAll("button").First(b => b.TextContent.Contains("Document Requirements")).Click();

        cut.FindAll("button").First(b => b.TextContent.Contains("Add Requirement")).Click();
        cut.Find("#ptd-req-type").Change("Passport");
        cut.Find("#ptd-req-ext").Change(".pdf");
        cut.Find("#ptd-req-size").Change("102400");
        cut.FindAll("button").First(b => b.TextContent.Contains("Save")).Click();

        _mediatorMock.Verify(m => m.Send(It.Is<AddDocumentRequirementCommand>(c =>
            c.PermitTypeId == id && c.DocumentType == "Passport"
            && c.AllowedExtensions.Contains(".pdf") && c.MaxFileSizeBytes == 102400), default), Times.Once);
    }

    [Fact]
    public void Should_SendUpdateDocumentRequirementCommand_WhenEditingRequirement()
    {
        var id = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        var dto = SampleDto(id);
        dto.DocumentRequirements = new List<FieldDefinitionDto>
        {
            new() { Id = reqId, Name = "ID Copy", Type = FieldType.FileUpload, IsRequired = true, AllowedExtensions = ".pdf", MaxFileSizeBytes = 102400 }
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default)).ReturnsAsync(dto);
        _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateDocumentRequirementCommand>(), default)).ReturnsAsync(true);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));
        cut.FindAll("button").First(b => b.TextContent.Contains("Document Requirements")).Click();

        cut.Find("button[title='Edit']").Click();
        cut.Find("#ptd-req-type").Change("National ID");
        cut.FindAll("button").First(b => b.TextContent.Contains("Save")).Click();

        _mediatorMock.Verify(m => m.Send(It.Is<UpdateDocumentRequirementCommand>(c =>
            c.PermitTypeId == id && c.RequirementId == reqId && c.IsRequired), default), Times.Once);
    }

    [Fact]
    public void Should_SendRemoveDocumentRequirementCommand_WhenRemovingRequirement()
    {
        var id = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        var dto = SampleDto(id);
        dto.DocumentRequirements = new List<FieldDefinitionDto>
        {
            new() { Id = reqId, Name = "Temp", Type = FieldType.FileUpload, IsRequired = false, AllowedExtensions = ".pdf", MaxFileSizeBytes = 102400 }
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default)).ReturnsAsync(dto);
        _mediatorMock.Setup(m => m.Send(It.IsAny<RemoveDocumentRequirementCommand>(), default)).ReturnsAsync(true);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));
        cut.FindAll("button").First(b => b.TextContent.Contains("Document Requirements")).Click();

        cut.Find("button[title='Remove']").Click();

        _mediatorMock.Verify(m => m.Send(It.Is<RemoveDocumentRequirementCommand>(c =>
            c.PermitTypeId == id && c.RequirementId == reqId), default), Times.Once);
    }

    [Fact]
    public void Should_SendMoveDocumentRequirementCommand_WhenReordering()
    {
        var id = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var dto = SampleDto(id);
        dto.DocumentRequirements = new List<FieldDefinitionDto>
        {
            new() { Id = firstId, Name = "First", Type = FieldType.FileUpload, IsRequired = false, AllowedExtensions = ".pdf", MaxFileSizeBytes = 1024 },
            new() { Id = secondId, Name = "Second", Type = FieldType.FileUpload, IsRequired = false, AllowedExtensions = ".pdf", MaxFileSizeBytes = 1024 }
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default)).ReturnsAsync(dto);
        _mediatorMock.Setup(m => m.Send(It.IsAny<MoveDocumentRequirementCommand>(), default)).ReturnsAsync(true);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));
        cut.FindAll("button").First(b => b.TextContent.Contains("Document Requirements")).Click();

        cut.FindAll("button[title='Move down']")[0].Click();

        _mediatorMock.Verify(m => m.Send(It.Is<MoveDocumentRequirementCommand>(c =>
            c.PermitTypeId == id && c.RequirementId == firstId && c.NewOrder == 2), default), Times.Once);
    }

    [Fact]
    public void Should_RenderDynamicFormPreview_WhenPreviewTabSelected()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDtoWithFields(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        cut.FindAll("button").First(b => b.TextContent.Contains("Preview")).Click();

        var generator = cut.FindComponent<DynamicFormGenerator>();
        Assert.NotNull(generator);
        Assert.Contains("Applicant Name", cut.Markup);
        Assert.Contains("Category", cut.Markup);
    }

    [Fact]
    public void Should_ShowEmptyPreviewMessage_WhenNoFields()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));

        cut.FindAll("button").First(b => b.TextContent.Contains("Preview")).Click();

        Assert.Contains("Add fields and document requirements", cut.Markup);
    }

    [Fact]
    public void Should_NavigateToDetailOnCancel_WithoutGuard_WhenDirty()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDto(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));
        var nav = Services.GetRequiredService<NavigationManager>();

        cut.Find("#ptd-name").Input("Changed Name");
        cut.FindAll("button").First(b => b.TextContent.Contains("Cancel")).Click();

        Assert.EndsWith($"/admin/permit-types/{id}", nav.Uri);
    }

    [Theory]
    [InlineData(FieldType.Text)]
    [InlineData(FieldType.MultilineText)]
    [InlineData(FieldType.Number)]
    [InlineData(FieldType.Date)]
    [InlineData(FieldType.Boolean)]
    [InlineData(FieldType.Dropdown)]
    [InlineData(FieldType.FileUpload)]
    public void Should_SendAddPermitFieldCommand_ForEveryFieldType(FieldType type)
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default)).ReturnsAsync(SampleDto(id));
        _mediatorMock.Setup(m => m.Send(It.IsAny<AddPermitFieldCommand>(), default)).ReturnsAsync(true);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));
        cut.FindAll("button").First(b => b.TextContent.Contains("Fields")).Click();

        cut.FindAll("button").First(b => b.TextContent.Contains("Add Field")).Click();
        cut.Find("#ptd-field-name").Change($"Field {type}");
        cut.Find("#ptd-field-type").Change(type.ToString());
        cut.FindAll("button").First(b => b.TextContent.Contains("Save")).Click();

        _mediatorMock.Verify(m => m.Send(It.Is<AddPermitFieldCommand>(c =>
            c.PermitTypeId == id && c.Name == $"Field {type}" && c.Type == type), default), Times.Once);
    }

    [Fact]
    public void Should_RenderPreviewInReadOnlyMode()
    {
        var id = Guid.NewGuid();
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default))
            .ReturnsAsync(SampleDtoWithFields(id));

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));
        cut.FindAll("button").First(b => b.TextContent.Contains("Preview")).Click();

        var generator = cut.FindComponent<DynamicFormGenerator>();
        Assert.Equal(FormFieldMode.ReadOnly, generator.Instance.Mode);
    }

    [Fact]
    public void Should_ReflectCurrentDesignerStateInPreview()
    {
        var id = Guid.NewGuid();
        var dto = SampleDto(id);
        dto.Fields = new List<FieldDefinitionDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Applicant Name", Type = FieldType.Text, IsRequired = true },
            new() { Id = Guid.NewGuid(), Name = "Site Plan", Type = FieldType.FileUpload, IsRequired = true, AllowedExtensions = ".pdf", MaxFileSizeBytes = 512000 }
        };
        dto.DocumentRequirements = new List<FieldDefinitionDto>
        {
            new() { Id = Guid.NewGuid(), Name = "ID Copy", Type = FieldType.FileUpload, IsRequired = true, AllowedExtensions = ".pdf", MaxFileSizeBytes = 102400 }
        };
        _mediatorMock.Setup(m => m.Send(It.IsAny<GetPermitTypeByIdQuery>(), default)).ReturnsAsync(dto);

        var cut = Render<PermitTypeDesigner>(parameters => parameters.Add(p => p.Id, id.ToString()));
        cut.FindAll("button").First(b => b.TextContent.Contains("Preview")).Click();

        var generator = cut.FindComponent<DynamicFormGenerator>();
        // Preview merges fields then document requirements (3 total).
        Assert.Equal(3, generator.Instance.Fields.Count);
        Assert.Contains("Applicant Name", cut.Markup);
        Assert.Contains("Site Plan", cut.Markup);
        Assert.Contains("ID Copy", cut.Markup);
    }
}
