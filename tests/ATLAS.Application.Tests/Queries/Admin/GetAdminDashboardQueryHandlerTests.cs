using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.EmailTemplates;
using ATLAS.Application.Queries.Admin;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries.Admin
{
    public class GetAdminDashboardQueryHandlerTests
    {
        private readonly Mock<IPermitTypeRepository> _mockPermitTypeRepository;
        private readonly Mock<IApplicationRepository> _mockApplicationRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IEmailTemplateStore> _mockEmailTemplateStore;
        private readonly GetAdminDashboardQueryHandler _handler;

        public GetAdminDashboardQueryHandlerTests()
        {
            _mockPermitTypeRepository = new Mock<IPermitTypeRepository>();
            _mockApplicationRepository = new Mock<IApplicationRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockEmailTemplateStore = new Mock<IEmailTemplateStore>();
            _mockEmailTemplateStore
                .Setup(s => s.GetTemplateNamesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>
                {
                    "SubmissionConfirmation",
                    "ApprovalNotification",
                    "RejectionNotification",
                    "InfoRequestNotification"
                });
            _handler = new GetAdminDashboardQueryHandler(
                _mockPermitTypeRepository.Object,
                _mockApplicationRepository.Object,
                _mockUserRepository.Object,
                _mockEmailTemplateStore.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnSummaryCounts()
        {
            // Arrange
            var permitTypes = new List<PermitType>
            {
                new PermitType("Building Permit", "desc", 100m),
                new PermitType("Event Permit", "desc", 50m)
            };
            var applications = new List<ATLAS.Domain.Entities.Application>
            {
                new ATLAS.Domain.Entities.Application(Guid.NewGuid(), permitTypes[0].Id, "notes"),
                new ATLAS.Domain.Entities.Application(Guid.NewGuid(), permitTypes[1].Id, "notes"),
                new ATLAS.Domain.Entities.Application(Guid.NewGuid(), permitTypes[0].Id, "notes")
            };
            var officers = new List<User>
            {
                new User(Guid.NewGuid(), "o1@atlas.test", "O", "One", UserRole.Officer),
                new User(Guid.NewGuid(), "o2@atlas.test", "O", "Two", UserRole.Officer)
            };

            _mockPermitTypeRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(permitTypes);
            _mockApplicationRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(applications);
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Officer, It.IsAny<CancellationToken>())).ReturnsAsync(officers);

            // Act
            var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

            // Assert
            Assert.Equal(2, result.PermitTypeCount);
            Assert.Equal(3, result.ApplicationCount);
            Assert.Equal(2, result.OfficerCount);
            Assert.Equal(4, result.ActiveEmailTemplateCount);
        }

        [Fact]
        public async Task Handle_WithNoData_ShouldReturnZeroCounts()
        {
            // Arrange
            _mockPermitTypeRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PermitType>());
            _mockApplicationRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ATLAS.Domain.Entities.Application>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Officer, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());

            // Act
            var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

            // Assert
            Assert.Equal(0, result.PermitTypeCount);
            Assert.Equal(0, result.ApplicationCount);
            Assert.Equal(0, result.OfficerCount);
            Assert.Equal(4, result.ActiveEmailTemplateCount);
        }

        [Fact]
        public async Task Handle_ShouldExcludeNonOfficerUsersFromOfficerCount()
        {
            // Arrange
            var users = new List<User>
            {
                new User(Guid.NewGuid(), "c@atlas.test", "C", "Cit", UserRole.Citizen),
                new User(Guid.NewGuid(), "o@atlas.test", "O", "Off", UserRole.Officer),
                new User(Guid.NewGuid(), "a@atlas.test", "A", "Adm", UserRole.Admin)
            };
            _mockPermitTypeRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PermitType>());
            _mockApplicationRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ATLAS.Domain.Entities.Application>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Officer, It.IsAny<CancellationToken>())).ReturnsAsync(
                users.Where(u => u.Role == UserRole.Officer).ToList());

            // Act
            var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

            // Assert
            Assert.Equal(1, result.OfficerCount);
        }
    }
}

