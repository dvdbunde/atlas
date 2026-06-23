using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.Interfaces;
using ATLAS.Application.Queries.Applications;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries
{
    public class GetApplicationsQueryHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockAppRepo;
        private readonly Mock<IUserRepository> _mockUserRepo;
        private readonly Mock<IPermitTypeRepository> _mockPermitTypeRepo;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly GetApplicationsQueryHandler _handler;

        public GetApplicationsQueryHandlerTests()
        {
            _mockAppRepo = new Mock<IApplicationRepository>();
            _mockUserRepo = new Mock<IUserRepository>();
            _mockPermitTypeRepo = new Mock<IPermitTypeRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            // Default: Admin role (all filters apply; no auto-scoping)
            _mockCurrentUserService.Setup(s => s.Role).Returns("Admin");
            _mockCurrentUserService.Setup(s => s.UserId).Returns(Guid.NewGuid());
            _handler = new GetApplicationsQueryHandler(
                _mockAppRepo.Object,
                _mockUserRepo.Object,
                _mockPermitTypeRepo.Object,
                _mockCurrentUserService.Object);
        }

        [Fact]
        public async Task Handle_ShouldFilterByCitizenId()
        {
            // Arrange
            var citizenId = Guid.NewGuid();
            var applications = new List<Domain.Entities.Application>
            {
                new Domain.Entities.Application(citizenId, Guid.NewGuid(), "Notes1"),
                new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Notes2")
            };
            _mockAppRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(applications);

            var query = new GetApplicationsQuery { CitizenId = citizenId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.All(result, a => Assert.Equal(citizenId, a.CitizenId));
        }

        [Fact]
        public async Task Handle_ShouldReturnAll_WhenNoFiltersApplied()
        {
            // Arrange
            var applications = new List<Domain.Entities.Application>
            {
                new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Notes1"),
                new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Notes2")
            };
            _mockAppRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(applications);

            var query = new GetApplicationsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoMatches()
        {
            // Arrange
            var applications = new List<Domain.Entities.Application>
            {
                new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Notes1")
            };
            _mockAppRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(applications);

            var query = new GetApplicationsQuery { CitizenId = Guid.NewGuid() };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task Handle_CitizenRole_ShouldAutoScopeToOwnApplications()
        {
            // Arrange
            var citizenId = Guid.NewGuid();
            var applications = new List<Domain.Entities.Application>
            {
                new Domain.Entities.Application(citizenId, Guid.NewGuid(), "My app"),
                new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Someone else's app")
            };
            _mockAppRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(applications);
            _mockCurrentUserService.Setup(s => s.Role).Returns("Citizen");
            _mockCurrentUserService.Setup(s => s.UserId).Returns(citizenId);

            var query = new GetApplicationsQuery(); // No explicit filters

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.All(result, a => Assert.Equal(citizenId, a.CitizenId));
        }

        [Fact]
        public async Task Handle_OfficerRole_ShouldReturnAllSubmittedApplications()
        {
            // Arrange
            var officerId = Guid.NewGuid();
            var assignedApp = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Assigned");
            assignedApp.Submit();
            var unassignedApp = new Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Not assigned");
            unassignedApp.Submit();

            var applications = new List<Domain.Entities.Application> { assignedApp, unassignedApp };
            _mockAppRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(applications);
            _mockCurrentUserService.Setup(s => s.Role).Returns("Officer");
            _mockCurrentUserService.Setup(s => s.UserId).Returns(officerId);

            var query = new GetApplicationsQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            // MVP: Officers see all submitted applications. Reviews-based auto-scoping
            // requires the Reviews collection on each Application to be populated
            // by the domain, which is a future enhancement.
            Assert.Equal(2, result.Count());
        }
    }
}
