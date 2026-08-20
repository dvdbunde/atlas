using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.EmailTemplates;
using ATLAS.Application.EmailTemplates.Commands;
using ATLAS.Application.EmailTemplates.Queries;
using ATLAS.Blazor.Components.Pages.Admin;
using Bunit;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ATLAS.Blazor.Tests.Components.Pages.Admin;

public class EmailTemplatesPageTests : BunitContext
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly EmailTemplate _template = new() { Name = "ApprovalNotification", Content = "Default body" };

    public EmailTemplatesPageTests()
    {
        Services.AddSingleton(_mediatorMock.Object);
        Services.AddSingleton(NullLogger<EmailTemplates>.Instance);
    }

    private void SetupTemplates()
    {
        _mediatorMock
            .Setup(m => m.Send(It.IsAny<GetEmailTemplatesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<EmailTemplate> { _template });
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetEmailTemplateByNameQuery>(q => q.Name == _template.Name), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_template);
    }

    private void SelectTemplate(IRenderedComponent<EmailTemplates> cut)
    {
        _mediatorMock
            .Setup(m => m.Send(It.Is<GetEmailTemplateByNameQuery>(q => q.Name == _template.Name), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_template);
        cut.FindAll("button").Single(b => b.TextContent.Contains(_template.Name)).Click();
        cut.WaitForAssertion(() => Assert.Contains("Template content", cut.Markup));
    }

    [Fact]
    public void EmailTemplates_WhenTemplateSelected_ShouldRenderResetButton()
    {
        SetupTemplates();
        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        Assert.Contains("Reset to default", cut.Markup);
    }

    [Fact]
    public void ResetTemplate_ShouldSendResetCommand()
    {
        SetupTemplates();
        var cut = Render<EmailTemplates>();
        SelectTemplate(cut);

        _mediatorMock
            .Setup(m => m.Send(It.IsAny<ResetEmailTemplateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        cut.FindAll("button").Single(b => b.TextContent.Contains("Reset to default")).Click();

        _mediatorMock.Verify(
            m => m.Send(It.Is<ResetEmailTemplateCommand>(c => c.Name == _template.Name), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
