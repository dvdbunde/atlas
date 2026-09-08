using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.EmailTemplates;
using ATLAS.Application.Queries.Admin;
using ATLAS.Application.Queries.AuditLogs;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using MediatR;
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
        private readonly Mock<IMediator> _mockMediator;
        private readonly GetAdminDashboardQueryHandler _handler;

        public GetAdminDashboardQueryHandlerTests()
        {
            _mockPermitTypeRepository = new Mock<IPermitTypeRepository>();
            _mockApplicationRepository = new Mock<IApplicationRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockEmailTemplateStore = new Mock<IEmailTemplateStore>();
            _mockMediator = new Mock<IMediator>();
            _mockEmailTemplateStore
                .Setup(s => s.GetTemplateNamesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<string>
                {
                    "SubmissionConfirmation",
                    "ApprovalNotification",
                    "RejectionNotification",
                    "InfoRequestNotification"
                });

            // Default audit query: no events in the last 24 hours.
            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuditLogListResult
                {
                    Items = Array.Empty<AuditLogDto>(),
                    TotalCount = 0,
                    PageNumber = 1,
                    PageSize = 1
                });

            // Default operations: healthy, checked now.
            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetOperationsOverviewQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new OperationsOverviewDto
                {
                    OverallHealth = "Healthy",
                    HealthUnavailable = false,
                    CapturedAtUtc = DateTime.UtcNow
                });

            _handler = new GetAdminDashboardQueryHandler(
                _mockPermitTypeRepository.Object,
                _mockApplicationRepository.Object,
                _mockUserRepository.Object,
                _mockEmailTemplateStore.Object,
                _mockMediator.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnSummaryCounts()
        {
            // Arrange
            var permitTypes = new List<PermitType>
            {
                new PermitType("Building Permit", "desc", 100m),
                new PermitType("Event Permit", "desc", 50m),
                new PermitType("Inactive Permit", "desc", 25m)
            };
            // Deactivate the third permit type for active/inactive split.
            permitTypes[2].Deactivate();

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
            var admins = new List<User>
            {
                new User(Guid.NewGuid(), "a1@atlas.test", "A", "One", UserRole.Admin)
            };
            var citizens = new List<User>
            {
                new User(Guid.NewGuid(), "c1@atlas.test", "C", "One", UserRole.Citizen),
                new User(Guid.NewGuid(), "c2@atlas.test", "C", "Two", UserRole.Citizen),
                new User(Guid.NewGuid(), "c3@atlas.test", "C", "Three", UserRole.Citizen)
            };

            _mockPermitTypeRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(permitTypes);
            _mockApplicationRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(applications);
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Officer, It.IsAny<CancellationToken>())).ReturnsAsync(officers);
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>())).ReturnsAsync(admins);
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Citizen, It.IsAny<CancellationToken>())).ReturnsAsync(citizens);

            // Act
            var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

            // Assert
            Assert.Equal(3, result.PermitTypeCount);
            Assert.Equal(2, result.ActivePermitTypeCount);
            Assert.Equal(1, result.InactivePermitTypeCount);
            Assert.Equal(3, result.ApplicationCount);
            // All three applications are Draft, so only the Draft status is non-zero.
            Assert.Equal(1, result.ApplicationStatusCounts.Count);
            Assert.Equal(3, result.ApplicationStatusCounts[ApplicationStatus.Draft]);
            Assert.Equal(6, result.UserCount);
            Assert.Equal(2, result.OfficerCount);
            Assert.Equal(1, result.AdminCount);
            Assert.Equal(3, result.CitizenCount);
            Assert.Equal(0, result.AuditLogEventsLast24Hours);
            Assert.Null(result.LatestAuditEventUtc);
            Assert.Equal(4, result.EmailTemplateCount);
            Assert.Equal("Healthy", result.OverallHealth);
        }

        [Fact]
        public async Task Handle_ShouldReturnActiveAndInactivePermitTypeCounts()
        {
            // Arrange
            var permitTypes = new List<PermitType>
            {
                new PermitType("Active 1", "desc", 10m),
                new PermitType("Active 2", "desc", 20m)
            };
            permitTypes[1].Deactivate();

            _mockPermitTypeRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(permitTypes);
            _mockApplicationRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ATLAS.Domain.Entities.Application>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Officer, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Citizen, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());

            // Act
            var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

            // Assert
            Assert.Equal(2, result.PermitTypeCount);
            Assert.Equal(1, result.ActivePermitTypeCount);
            Assert.Equal(1, result.InactivePermitTypeCount);
        }

        [Fact]
        public async Task Handle_ShouldCountApplicationsByDefinedStatuses()
        {
            // Arrange
            var app1 = new ATLAS.Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "notes");
            var app2 = new ATLAS.Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "notes");
            var app3 = new ATLAS.Domain.Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "notes");
            app1.Submit();
            app2.Submit();
            var officerId = Guid.NewGuid();
            app3.Submit();
            // AssignToOfficer on a submitted app transitions it to Under Review
            // and assigns the officer, making it eligible for approval.
            app3.AssignToOfficer(officerId);
            app3.Approve(officerId, "ok");
            _mockApplicationRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ATLAS.Domain.Entities.Application> { app1, app2, app3 });
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Officer, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Citizen, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());

            // Act
            var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

            // Assert — only non-zero statuses appear, from the defined status set.
            Assert.Equal(3, result.ApplicationCount);
            Assert.Equal(2, result.ApplicationStatusCounts[ApplicationStatus.Submitted]);
            Assert.Equal(1, result.ApplicationStatusCounts[ApplicationStatus.Approved]);
            Assert.False(result.ApplicationStatusCounts.ContainsKey(ApplicationStatus.Draft));
            Assert.Contains(ApplicationStatus.Submitted, result.ApplicationStatusCounts.Keys);
            Assert.Contains(ApplicationStatus.Approved, result.ApplicationStatusCounts.Keys);
            // Rendered statuses follow the defined lifecycle order (Submitted before Approved).
            Assert.Equal(
                new[] { ApplicationStatus.Submitted, ApplicationStatus.Approved },
                result.ApplicationStatusCounts.Keys);
        }

        [Fact]
        public async Task Handle_ShouldReturnAuditRecentActivity()
        {
            // Arrange
            var latest = DateTime.UtcNow.AddHours(-1);
            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AuditLogListResult
                {
                    Items = new[]
                    {
                        new AuditLogDto
                        {
                            Id = Guid.NewGuid(),
                            Action = "View",
                            EntityType = "PermitType",
                            Timestamp = latest
                        }
                    },
                    TotalCount = 12,
                    PageNumber = 1,
                    PageSize = 1
                });

            _mockPermitTypeRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PermitType>());
            _mockApplicationRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ATLAS.Domain.Entities.Application>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Officer, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Citizen, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());

            // Act
            var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

            // Assert
            Assert.Equal(12, result.AuditLogEventsLast24Hours);
            Assert.NotNull(result.LatestAuditEventUtc);
            Assert.Equal(latest, result.LatestAuditEventUtc);
        }

        [Fact]
        public async Task Handle_WithAuditOrHealthFailure_ShouldStillReturnDashboard()
        {
            // Arrange
            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("audit unavailable"));
            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetOperationsOverviewQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("health unavailable"));

            _mockPermitTypeRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PermitType>());
            _mockApplicationRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ATLAS.Domain.Entities.Application>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Officer, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Citizen, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());

            // Act
            var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

            // Assert — human/dashboard still renders with supporting metrics unavailable.
            Assert.Equal(0, result.AuditLogEventsLast24Hours);
            Assert.Null(result.LatestAuditEventUtc);
            Assert.Null(result.OverallHealth);
            Assert.Null(result.HealthCheckedAtUtc);
        }

        [Fact]
        public async Task Handle_WithNoData_ShouldReturnZeroCounts()
        {
            // Arrange
            _mockPermitTypeRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PermitType>());
            _mockApplicationRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ATLAS.Domain.Entities.Application>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Officer, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Citizen, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());

            // Act
            var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

            // Assert
            Assert.Equal(0, result.PermitTypeCount);
            Assert.Equal(0, result.ApplicationCount);
            Assert.Equal(0, result.OfficerCount);
            Assert.Equal(0, result.AdminCount);
            Assert.Equal(0, result.CitizenCount);
            Assert.Equal(0, result.UserCount);
            Assert.Equal(4, result.EmailTemplateCount);
            Assert.Equal("Healthy", result.OverallHealth);
        }

        [Fact]
        public async Task Handle_ShouldExcludeUsersFromWrongRoleCounts()
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
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>())).ReturnsAsync(
                users.Where(u => u.Role == UserRole.Admin).ToList());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Citizen, It.IsAny<CancellationToken>())).ReturnsAsync(
                users.Where(u => u.Role == UserRole.Citizen).ToList());

            // Act
            var result = await _handler.Handle(new GetAdminDashboardQuery(), CancellationToken.None);

            // Assert
            Assert.Equal(3, result.UserCount);
            Assert.Equal(1, result.OfficerCount);
            Assert.Equal(1, result.AdminCount);
            Assert.Equal(1, result.CitizenCount);
        }

        [Fact]
        public async Task Handle_WhenAuditQueryCancelled_ShouldLetCancellationPropagate()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var token = cts.Token;

            _mockPermitTypeRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PermitType>());
            _mockApplicationRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ATLAS.Domain.Entities.Application>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Officer, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Citizen, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetAuditLogsQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException(token));

            // Act & Assert — cancellation must propagate, not be swallowed.
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(new GetAdminDashboardQuery(), token));
        }

        [Fact]
        public async Task Handle_WhenOperationsQueryCancelled_ShouldLetCancellationPropagate()
        {
            // Arrange
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            var token = cts.Token;

            _mockPermitTypeRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<PermitType>());
            _mockApplicationRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<ATLAS.Domain.Entities.Application>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Officer, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Admin, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockUserRepository.Setup(r => r.GetByRoleAsync(UserRole.Citizen, It.IsAny<CancellationToken>())).ReturnsAsync(new List<User>());
            _mockMediator
                .Setup(m => m.Send(It.IsAny<GetOperationsOverviewQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException(token));

            // Act & Assert — cancellation must propagate, not be swallowed.
            await Assert.ThrowsAsync<OperationCanceledException>(
                () => _handler.Handle(new GetAdminDashboardQuery(), token));
        }
    }
}