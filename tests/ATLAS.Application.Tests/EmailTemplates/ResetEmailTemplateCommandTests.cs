//----------------------
// ResetEmailTemplateCommand Tests
// Verifies that the reset command deletes the customization (delegating to the store
// reset), returns false for unknown/missing templates, and raises the audit event.
//----------------------

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.EmailTemplates;
using ATLAS.Application.EmailTemplates.Commands;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Email;
using MediatR;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.EmailTemplates
{
    public class ResetEmailTemplateCommandTests
    {
        private readonly Mock<IEmailTemplateStore> _storeMock = new();
        private readonly Mock<IMediator> _mediatorMock = new();
        private readonly Mock<ICurrentUserService> _userMock = new();
        private readonly ResetEmailTemplateCommandHandler _handler;

        public ResetEmailTemplateCommandTests()
        {
            _handler = new ResetEmailTemplateCommandHandler(
                _storeMock.Object, _mediatorMock.Object, _userMock.Object);
        }

        [Fact]
        public async Task Handle_DeletesCustomization_AndPublishesResetEvent()
        {
            _storeMock.Setup(s => s.GetByNameAsync("ApprovalNotification", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new EmailTemplate { Name = "ApprovalNotification", Content = "default" });

            var result = await _handler.Handle(new ResetEmailTemplateCommand("ApprovalNotification"), default);

            Assert.True(result);
            _storeMock.Verify(s => s.ResetAsync("ApprovalNotification", It.IsAny<CancellationToken>()), Times.Once);
            _mediatorMock.Verify(m => m.Publish(
                It.Is<EmailTemplateResetEvent>(e => e.TemplateName == "ApprovalNotification"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_ReturnsFalse_WhenTemplateMissing()
        {
            _storeMock.Setup(s => s.GetByNameAsync("ApprovalNotification", It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailTemplate?)null);

            var result = await _handler.Handle(new ResetEmailTemplateCommand("ApprovalNotification"), default);

            Assert.False(result);
            _storeMock.Verify(s => s.ResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _mediatorMock.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_DoesNotPublishEvent_WhenStoreMissing()
        {
            _storeMock.Setup(s => s.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((EmailTemplate?)null);

            await _handler.Handle(new ResetEmailTemplateCommand("ApprovalNotification"), default);

            _mediatorMock.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
