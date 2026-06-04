using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries;
using Entities = ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries
{
    public class GetApplicationByIdQueryHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly GetApplicationByIdQueryHandler _handler;

        public GetApplicationByIdQueryHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _handler = new GetApplicationByIdQueryHandler(_mockRepository.Object);
        }

        [Fact]
        public async Task Handle_ValidId_ShouldReturnApplicationDetailDto()
        {
            // Arrange
            var application = new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Test notes");
            application.Submit(); // This sets Status to Submitted and generates ApplicationNumber
            
            _mockRepository.Setup(r => r.GetByIdAsync(application.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);

            var query = new GetApplicationByIdQuery { ApplicationId = application.Id };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(application.Id, result.Id);
            Assert.False(string.IsNullOrEmpty(result.ApplicationNumber));
            Assert.Equal((int)ApplicationStatus.Submitted, result.Status);
        }

        [Fact]
        public async Task Handle_ApplicationNotFound_ShouldReturnNull()
        {
            // Arrange
            var applicationId = Guid.NewGuid();
            _mockRepository.Setup(r => r.GetByIdAsync(applicationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Entities.Application)null);

            var query = new GetApplicationByIdQuery { ApplicationId = applicationId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }
    }
}
