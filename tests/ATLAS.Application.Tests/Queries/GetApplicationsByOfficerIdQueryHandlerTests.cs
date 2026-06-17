using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.Applications;
using Entities = ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries
{
    public class GetApplicationsByOfficerIdQueryHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockAppRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IPermitTypeRepository> _mockPermitTypeRepo;
        private readonly GetApplicationsByOfficerIdQueryHandler _handler;

        public GetApplicationsByOfficerIdQueryHandlerTests()
        {
            _mockAppRepo = new Mock<IApplicationRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockPermitTypeRepo = new Mock<IPermitTypeRepository>();
            _handler = new GetApplicationsByOfficerIdQueryHandler(
                _mockAppRepo.Object,
                _mockUserRepo.Object,
                _mockPermitTypeRepo.Object);
        }

        [Fact]
        public async Task Handle_ValidOfficerId_ShouldReturnApplications()
        {
            // Arrange
            var officerId = Guid.NewGuid();
            var applications = new List<Entities.Application>
            {
                new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Notes 1"),
                new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Notes 2")
            };
            applications[0].Submit();
            applications[1].Submit();

            _mockAppRepo.Setup(r => r.GetByOfficerIdAsync(officerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(applications);

            var query = new GetApplicationsByOfficerIdQuery { OfficerId = officerId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task Handle_InvalidOfficerId_ShouldReturnEmptyList()
        {
            // Arrange
            var officerId = Guid.NewGuid();
            _mockAppRepo.Setup(r => r.GetByOfficerIdAsync(officerId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Entities.Application>());

            var query = new GetApplicationsByOfficerIdQuery { OfficerId = officerId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new GetApplicationsByOfficerIdQueryHandler(
                    null,
                    new Mock<IUserRepository>().Object,
                    new Mock<IPermitTypeRepository>().Object));
        }
    }
}
