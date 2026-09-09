using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Queries.Admin;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries.Admin
{
    public class GetAdminApplicationsQueryHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockAppRepository;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IPermitTypeRepository> _mockPermitTypeRepository;
        private readonly GetAdminApplicationsQueryHandler _handler;
        private readonly Guid _citizenId;
        private readonly Guid _permitTypeId;

        public GetAdminApplicationsQueryHandlerTests()
        {
            _mockAppRepository = new Mock<IApplicationRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockPermitTypeRepository = new Mock<IPermitTypeRepository>();
            _citizenId = Guid.NewGuid();
            _permitTypeId = Guid.NewGuid();

            _mockUserRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);
            _mockPermitTypeRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((PermitType?)null);

            _handler = new GetAdminApplicationsQueryHandler(
                _mockAppRepository.Object,
                _mockUserRepository.Object,
                _mockPermitTypeRepository.Object);
        }

        private static void SetLastUpdated(Domain.Entities.Application app, DateTime value)
        {
            typeof(Domain.Entities.Application).GetProperty("ModifiedDate")!.SetValue(app, value);
        }

        private Domain.Entities.Application BuildApplication()
        {
            return new Domain.Entities.Application(_citizenId, _permitTypeId, "Notes");
        }

        [Fact]
        public async Task Handle_DateFromFilter_ShouldFilterByLastUpdated()
        {
            var older = BuildApplication();
            SetLastUpdated(older, DateTime.UtcNow.AddDays(-10));
            var newer = BuildApplication();
            SetLastUpdated(newer, DateTime.UtcNow);
            _mockAppRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Domain.Entities.Application> { older, newer });
            var query = new GetAdminApplicationsQuery
            {
                DateFrom = DateTime.UtcNow.AddDays(-5),
                SortBy = AdminApplicationSortBy.LastUpdated,
                SortDescending = false
            };
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.Equal(1, result.TotalCount);
            Assert.Equal(newer.Id, result.Items[0].ApplicationId);
        }

        [Fact]
        public async Task Handle_DateToFilter_ShouldFilterByLastUpdated()
        {
            var older = BuildApplication();
            SetLastUpdated(older, DateTime.UtcNow.AddDays(-10));
            var newer = BuildApplication();
            SetLastUpdated(newer, DateTime.UtcNow);
            _mockAppRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Domain.Entities.Application> { older, newer });
            var query = new GetAdminApplicationsQuery
            {
                DateTo = DateTime.UtcNow.AddDays(-5),
                SortBy = AdminApplicationSortBy.LastUpdated,
                SortDescending = false
            };
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.Equal(1, result.TotalCount);
            Assert.Equal(older.Id, result.Items[0].ApplicationId);
        }

        [Fact]
        public async Task Handle_SortByLastUpdated_ShouldUseLastUpdatedField()
        {
            var older = BuildApplication();
            SetLastUpdated(older, DateTime.UtcNow.AddDays(-10));
            var newer = BuildApplication();
            SetLastUpdated(newer, DateTime.UtcNow);
            _mockAppRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Domain.Entities.Application> { older, newer });
            var query = new GetAdminApplicationsQuery
            {
                SortBy = AdminApplicationSortBy.LastUpdated,
                SortDescending = false
            };
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(older.Id, result.Items[0].ApplicationId);
            Assert.Equal(newer.Id, result.Items[1].ApplicationId);
        }

        [Fact]
        public async Task Handle_DtoMapping_ShouldExposePersistedLastUpdated()
        {
            var app = BuildApplication();
            var lastUpdated = DateTime.UtcNow.AddHours(-3);
            SetLastUpdated(app, lastUpdated);
            _mockAppRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Domain.Entities.Application> { app });
            var query = new GetAdminApplicationsQuery();
            var result = await _handler.Handle(query, CancellationToken.None);
            Assert.Equal(1, result.TotalCount);
            Assert.Equal(lastUpdated, result.Items[0].LastUpdated);
        }
    }
}

