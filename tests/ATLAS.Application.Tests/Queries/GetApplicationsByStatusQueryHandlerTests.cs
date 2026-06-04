using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
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
    public class GetApplicationsByStatusQueryHandlerTests
    {
        private readonly Mock<IApplicationRepository> _mockRepository;
        private readonly GetApplicationsByStatusQueryHandler _handler;

        public GetApplicationsByStatusQueryHandlerTests()
        {
            _mockRepository = new Mock<IApplicationRepository>();
            _handler = new GetApplicationsByStatusQueryHandler(_mockRepository.Object);
        }

        [Fact]
        public async Task Handle_WithStatusFilter_ShouldReturnFilteredApplications()
        {
            // Arrange
            var applications = new List<Entities.Application>
            {
                new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Notes 1"),
                new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Notes 2"),
                new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Notes 3")
            };
            
            // Submit first two applications to set status to Submitted
            applications[0].Submit();
            applications[1].Submit();
            // Third remains in Draft status

            _mockRepository.Setup(r => r.GetByStatusAsync(ApplicationStatus.Submitted, It.IsAny<CancellationToken>()))
                .ReturnsAsync(applications.Where(a => a.Status == ApplicationStatus.Submitted).ToList());

            var query = new GetApplicationsByStatusQuery { Status = ApplicationStatus.Submitted };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.All(result, dto => Assert.Equal((int)ApplicationStatus.Submitted, dto.Status));
        }

        [Fact]
        public async Task Handle_WithoutStatusFilter_ShouldReturnAllApplications()
        {
            // Arrange
            var applications = new List<Entities.Application>
            {
                new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Notes 1"),
                new Entities.Application(Guid.NewGuid(), Guid.NewGuid(), "Notes 2")
            };

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(applications);

            var query = new GetApplicationsByStatusQuery { Status = null };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count);
        }
    }
}
