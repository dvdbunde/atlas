using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Interfaces;
using ATLAS.Application.Queries.Applications;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries
{
    public class GetCitizenDashboardQueryHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockAppRepository;
        private readonly Mock<IPermitTypeRepository> _mockPermitTypeRepository;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;
        private readonly Mock<ILogger<GetCitizenDashboardQueryHandler>> _mockLogger;
        private readonly GetCitizenDashboardQueryHandler _handler;
        private readonly Guid _testUserId;
        private readonly Guid _permitTypeId;

        public GetCitizenDashboardQueryHandlerTests()
        {
            _mockAppRepository = new Mock<IApplicationRepository>();
            _mockPermitTypeRepository = new Mock<IPermitTypeRepository>();
            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _mockLogger = new Mock<ILogger<GetCitizenDashboardQueryHandler>>();
            _testUserId = Guid.NewGuid();
            _permitTypeId = Guid.NewGuid();
            _mockCurrentUserService.Setup(s => s.UserId).Returns(_testUserId);
            _handler = new GetCitizenDashboardQueryHandler(
                _mockAppRepository.Object,
                _mockPermitTypeRepository.Object,
                _mockCurrentUserService.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnDashboardDtos()
        {
            // Arrange
            var applications = new List<Domain.Entities.Application>
            {
                new Domain.Entities.Application(_testUserId, _permitTypeId, "Notes1"),
                new Domain.Entities.Application(_testUserId, _permitTypeId, "Notes2")
            };
            _mockAppRepository.Setup(r => r.GetByCitizenIdAsync(_testUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(applications);
            _mockPermitTypeRepository.Setup(r => r.GetNameByIdAsync(_permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync("Building Permit");

            var query = new GetCitizenDashboardQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, dto => Assert.Equal("Building Permit", dto.PermitTypeName));
            Assert.All(result, dto => Assert.NotEqual(Guid.Empty, dto.ApplicationId));
            Assert.All(result, dto => Assert.NotNull(dto.ApplicationNumber));
        }

        [Fact]
        public async Task Handle_UnauthenticatedUser_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            _mockCurrentUserService.Setup(s => s.UserId).Returns((Guid?)null);
            var query = new GetCitizenDashboardQuery();

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _handler.Handle(query, CancellationToken.None));
        }

        [Fact]
        public async Task Handle_NoApplications_ShouldReturnEmpty()
        {
            // Arrange
            _mockAppRepository.Setup(r => r.GetByCitizenIdAsync(_testUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Domain.Entities.Application>());
            var query = new GetCitizenDashboardQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task Handle_UnknownPermitType_ShouldShowUnknownName()
        {
            // Arrange
            var applications = new List<Domain.Entities.Application>
            {
                new Domain.Entities.Application(_testUserId, _permitTypeId, "Notes")
            };
            _mockAppRepository.Setup(r => r.GetByCitizenIdAsync(_testUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(applications);
            _mockPermitTypeRepository.Setup(r => r.GetNameByIdAsync(_permitTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((string?)null); // permit type name not found

            var query = new GetCitizenDashboardQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var dto = Assert.Single(result);
            Assert.Equal("Unknown", dto.PermitTypeName);
        }
    }
}