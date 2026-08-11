//----------------------
// ApplicationSubmittedEmailHandler Tests
// Verifies the email flow: the handler renders the template via IEmailTemplateRenderer
// and passes the rendered content to IEmailService. This confirms the Phase A template
// store (customized/default) feeds the email sender.
//----------------------

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Events;
using ATLAS.Domain.Interfaces;
using ATLAS.Infrastructure.EventHandlers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ATLAS.Infrastructure.Tests.EventHandlers
{
    public class ApplicationSubmittedEmailHandlerTests
    {
        private readonly Mock<IEmailService> _emailServiceMock = new();
        private readonly Mock<IEmailTemplateRenderer> _rendererMock = new();
        private readonly Mock<IApplicationRepository> _applicationRepoMock = new();
        private readonly Mock<IUserRepository> _userRepoMock = new();
        private readonly Mock<IPermitTypeRepository> _permitTypeRepoMock = new();

        private readonly ApplicationSubmittedEmailHandler _handler;

        public ApplicationSubmittedEmailHandlerTests()
        {
            _handler = new ApplicationSubmittedEmailHandler(
                _emailServiceMock.Object,
                _rendererMock.Object,
                _applicationRepoMock.Object,
                _userRepoMock.Object,
                _permitTypeRepoMock.Object,
                NullLogger<ApplicationSubmittedEmailHandler>.Instance);
        }

        [Fact]
        public async Task Handle_RendersTemplate_AndSendsRenderedContent()
        {
            var applicationId = Guid.NewGuid();
            var citizenId = Guid.NewGuid();
            var permitTypeId = Guid.NewGuid();

            var application = new ATLAS.Domain.Entities.Application(citizenId, permitTypeId, "notes");
            application.Submit(); // sets Status = Submitted and SubmittedDate

            _applicationRepoMock.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _userRepoMock.Setup(r => r.GetByIdAsync(citizenId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new User(citizenId, "citizen@example.com", "Jane", "Citizen", UserRole.Citizen));
            _permitTypeRepoMock.Setup(r => r.GetNameByIdAsync(permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync("Building Permit");
            _rendererMock.Setup(r => r.RenderAsync("SubmissionConfirmation", It.IsAny<object>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("RENDERED BODY");

            await _handler.Handle(new ApplicationSubmittedEvent(applicationId, permitTypeId), CancellationToken.None);

            _rendererMock.Verify(r => r.RenderAsync("SubmissionConfirmation", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
            _emailServiceMock.Verify(e => e.SendAsync(
                "citizen@example.com",
                "Application Submitted Successfully",
                "RENDERED BODY",
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
