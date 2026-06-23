using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Queries.PermitTypes;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Queries
{
    public class GetActivePermitTypesQueryHandlerTests
    {
        private readonly Mock<IPermitTypeRepository> _mockRepository;
        private readonly Mock<ILogger<GetActivePermitTypesQueryHandler>> _mockLogger;
        private readonly GetActivePermitTypesQueryHandler _handler;

        public GetActivePermitTypesQueryHandlerTests()
        {
            _mockRepository = new Mock<IPermitTypeRepository>();
            _mockLogger = new Mock<ILogger<GetActivePermitTypesQueryHandler>>();
            _handler = new GetActivePermitTypesQueryHandler(
                _mockRepository.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task Handle_ShouldReturnActivePermitTypes()
        {
            // Arrange
            var permitTypes = new List<PermitType>
            {
                new PermitType("Building Permit", "For construction", 100.00m),
                new PermitType("Event Permit", "For events", 50.00m)
            };
            _mockRepository.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitTypes);

            var query = new GetActivePermitTypesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, dto => Assert.True(dto.IsActive));
        }

        [Fact]
        public async Task Handle_NoActiveTypes_ShouldReturnEmpty()
        {
            // Arrange
            _mockRepository.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PermitType>());

            var query = new GetActivePermitTypesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task Handle_ShouldMapAllProperties()
        {
            // Arrange
            var permitType = new PermitType("Building Permit", "Description here", 100.00m);
            var id = permitType.Id;
            _mockRepository.Setup(r => r.GetAllActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PermitType> { permitType });

            var query = new GetActivePermitTypesQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var dto = Assert.Single(result);
            Assert.Equal(id, dto.Id);
            Assert.Equal("Building Permit", dto.Name);
            Assert.Equal("Description here", dto.Description);
            Assert.Equal(100.00m, dto.Fee);
            Assert.True(dto.IsActive);
        }
    }
}