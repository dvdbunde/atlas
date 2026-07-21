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

        [Fact]
        public async Task Handle_WithSearchTerm_ShouldReturnOnlyMatchingNames()
        {
            // Arrange
            var permitTypes = new List<PermitType>
            {
                new PermitType("Building Permit", "Desc", 100.00m),
                new PermitType("Event Permit", "Desc", 200.00m),
                new PermitType("Building Extension", "Desc", 50.00m)
            };

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitTypes);

            var query = new GetPermitTypesQuery { IncludeInactive = true, SearchTerm = "building" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, pt => Assert.Contains("building", pt.Name, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Handle_WithActiveFilter_ShouldReturnOnlyActive()
        {
            // Arrange
            var permitTypes = new List<PermitType>
            {
                new PermitType("Active Type", "Desc", 100.00m),
                new PermitType("Inactive Type", "Desc", 200.00m)
            };
            permitTypes[1].Deactivate(Guid.NewGuid());

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitTypes);

            var query = new GetPermitTypesQuery { IncludeInactive = true, ActiveOnly = true };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.All(result, pt => Assert.True(pt.IsActive));
        }

        [Fact]
        public async Task Handle_WithInactiveFilter_ShouldReturnOnlyInactive()
        {
            // Arrange
            var permitTypes = new List<PermitType>
            {
                new PermitType("Active Type", "Desc", 100.00m),
                new PermitType("Inactive Type", "Desc", 200.00m)
            };
            permitTypes[1].Deactivate(Guid.NewGuid());

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitTypes);

            var query = new GetPermitTypesQuery { IncludeInactive = true, InactiveOnly = true };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.Single(result);
            Assert.All(result, pt => Assert.False(pt.IsActive));
        }

        [Fact]
        public async Task Handle_WithSortByName_ShouldReturnOrderedByName()
        {
            // Arrange
            var permitTypes = new List<PermitType>
            {
                new PermitType("Charlie", "Desc", 100.00m),
                new PermitType("Alpha", "Desc", 200.00m),
                new PermitType("Bravo", "Desc", 50.00m)
            };

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(permitTypes);

            var query = new GetPermitTypesQuery { IncludeInactive = true, SortBy = PermitTypeSortOption.NameAsc };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var names = result.Select(r => r.Name).ToList();
            Assert.Equal(new[] { "Alpha", "Bravo", "Charlie" }, names);
        }

        [Fact]
        public async Task Handle_ShouldMapFieldAndDocumentRequirementCounts()
        {
            // Arrange
            var permitType = new PermitType("Typed", "Desc", 100.00m);
            permitType.AddField("ApplicantName", ATLAS.Domain.Enums.FieldType.Text, true);
            permitType.AddDocumentRequirement("Passport", true, new[] { ".pdf" }, 26214400);

            _mockRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<PermitType> { permitType });

            var query = new GetPermitTypesQuery { IncludeInactive = true };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            var dto = Assert.Single(result);
            Assert.Equal(1, dto.FieldCount);
            Assert.Equal(1, dto.DocumentRequirementCount);
        }
    }
}
