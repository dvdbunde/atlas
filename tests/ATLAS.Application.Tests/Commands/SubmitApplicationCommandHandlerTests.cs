using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Commands;
using Entities = ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Commands
{
    public class SubmitApplicationCommandHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly Mock<IMediator> _mockMediator;
        private readonly SubmitApplicationCommandHandler _handler;

        public SubmitApplicationCommandHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _mockMediator = new Mock<IMediator>();
            _handler = new SubmitApplicationCommandHandler(_mockRepository.Object, _mockMediator.Object);
        }

        [Fact]
        public async Task Handle_ValidCommand_ShouldCreateApplicationAndReturnId()
        {
            // Arrange
            var command = new SubmitApplicationCommand
            {
                CitizenId = Guid.NewGuid(),
                PermitTypeId = Guid.NewGuid(),
                CitizenNotes = "Test notes"
            };

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotEqual(Guid.Empty, result);
            _mockRepository.Verify(r => r.AddAsync(It.IsAny<Entities.Application>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockMediator.Verify(m => m.Publish(It.IsAny<INotification>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Handle_NullCommand_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsAsync<ArgumentNullException>(
                () => _handler.Handle(null!, CancellationToken.None));
        }
    }
}
