using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.DTOs;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries
{
    public class GetPermitTypesQueryHandlerTests
    {
        private readonly Mock<IPermitTypeRepository> _mockRepository;
        private readonly GetPermitTypesQueryHandler _handler;

        public GetPermitTypesQueryHandlerTests()
        {
            _mockRepository = new Mock<IPermitTypeRepository>();
            _handler = new GetPermitTypesQueryHandler(_mockRepository.Object);
        }

        [Fact]
        public async Task Handle_IncludeInactiveTrue_ShouldReturnAllPermitTypes()
        {
            // Arrange
            var permitTypes = new List<PermitType>
            {
                new PermitType("Type1", "Description1", 100.00m),
                new PermitType("Type2", "Description2", 200.00m)
            };
            // First is active by default, deactivate second
            permitTypes[1].Deactivate(Guid.NewGuid());

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitTypes);

            var query = new GetPermitTypesQuery { IncludeInactive = true };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task Handle_IncludeInactiveFalse_ShouldReturnOnlyActive()
        {
            // Arrange
            var permitTypes = new List<PermitType>
            {
                new PermitType("Type1", "Description1", 100.00m),
                new PermitType("Type2", "Description2", 200.00m)
            };
            // First is active by default, deactivate second
            permitTypes[1].Deactivate(Guid.NewGuid());

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitTypes);

            var query = new GetPermitTypesQuery { IncludeInactive = false };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.All(result, pt => Assert.True(pt.IsActive));
        }

        [Fact]
        public async Task Handle_DefaultIncludeInactive_ShouldReturnOnlyActive()
        {
            // Arrange
            var permitTypes = new List<PermitType>
            {
                new PermitType("Type1", "Description1", 100.00m),
                new PermitType("Type2", "Description2", 200.00m)
            };
            // First is active by default, deactivate second
            permitTypes[1].Deactivate(Guid.NewGuid());

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitTypes);

            var query = new GetPermitTypesQuery(); // Default IncludeInactive = false

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.All(result, pt => Assert.True(pt.IsActive));
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRepositoryIsNull()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GetPermitTypesQueryHandler(null));
        }
    }
}
