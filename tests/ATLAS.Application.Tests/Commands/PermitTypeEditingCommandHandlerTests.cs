using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Commands.PermitTypes;
using ATLAS.Domain.Entities;
using ATLAS.Domain.Enums;
using ATLAS.Domain.Interfaces;
using Moq;
using Xunit;

namespace ATLAS.Application.Tests.Commands
{
    public class PermitTypeEditingCommandHandlerTests
    {
        private readonly Mock<IPermitTypeRepository> _mockRepository = new();
        private readonly CancellationToken _ct = CancellationToken.None;

        [Fact]
        public async Task AddPermitField_WhenFound_ShouldAddAndReturnTrue()
        {
            var permitType = new PermitType("Building Permit", "Desc", 100m);
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _ct)).ReturnsAsync(permitType);

            var handler = new AddPermitFieldCommandHandler(_mockRepository.Object);
            var result = await handler.Handle(
                new AddPermitFieldCommand { PermitTypeId = permitType.Id, Name = "NewField", Type = FieldType.Text, IsRequired = true },
                _ct);

            Assert.True(result);
            Assert.Single(permitType.Fields);
            Assert.Equal("NewField", permitType.Fields[0].Name);
            _mockRepository.Verify(r => r.UpdateAsync(permitType, _ct), Times.Once);
        }

        [Fact]
        public async Task AddPermitField_WhenNotFound_ShouldReturnFalse()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _ct)).ReturnsAsync((PermitType)null);
            var handler = new AddPermitFieldCommandHandler(_mockRepository.Object);

            var result = await handler.Handle(
                new AddPermitFieldCommand { PermitTypeId = Guid.NewGuid(), Name = "X", Type = FieldType.Text, IsRequired = true },
                _ct);

            Assert.False(result);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PermitType>(), _ct), Times.Never);
        }

        [Fact]
        public async Task AddDocumentRequirement_WhenFound_ShouldAddAndReturnTrue()
        {
            var permitType = new PermitType("Building Permit", "Desc", 100m);
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _ct)).ReturnsAsync(permitType);

            var handler = new AddDocumentRequirementCommandHandler(_mockRepository.Object);
            var result = await handler.Handle(
                new AddDocumentRequirementCommand { PermitTypeId = permitType.Id, DocumentType = "ID", IsRequired = true, AllowedExtensions = new[] { ".pdf" }, MaxFileSizeBytes = 1000 },
                _ct);

            Assert.True(result);
            Assert.Single(permitType.DocumentRequirements);
            Assert.Equal("ID", permitType.DocumentRequirements[0].DocumentType);
            _mockRepository.Verify(r => r.UpdateAsync(permitType, _ct), Times.Once);
        }

        [Fact]
        public async Task AddDocumentRequirement_WhenNotFound_ShouldReturnFalse()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _ct)).ReturnsAsync((PermitType)null);
            var handler = new AddDocumentRequirementCommandHandler(_mockRepository.Object);

            var result = await handler.Handle(
                new AddDocumentRequirementCommand { PermitTypeId = Guid.NewGuid(), DocumentType = "ID", IsRequired = true, AllowedExtensions = new[] { ".pdf" }, MaxFileSizeBytes = 1000 },
                _ct);

            Assert.False(result);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PermitType>(), _ct), Times.Never);
        }

        [Fact]
        public async Task UpdatePermitField_WhenFound_ShouldUpdateAndReturnTrue()
        {
            var permitType = new PermitType("Building Permit", "Desc", 100m);
            permitType.AddField("FieldA", FieldType.Text, true);
            var fieldId = permitType.Fields[0].Id;
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _ct)).ReturnsAsync(permitType);

            var handler = new UpdatePermitFieldCommandHandler(_mockRepository.Object);
            var result = await handler.Handle(
                new UpdatePermitFieldCommand { PermitTypeId = permitType.Id, FieldId = fieldId, Name = "Renamed", Type = FieldType.Number, IsRequired = false },
                _ct);

            Assert.True(result);
            Assert.Equal("Renamed", permitType.Fields[0].Name);
            _mockRepository.Verify(r => r.UpdateAsync(permitType, _ct), Times.Once);
        }

        [Fact]
        public async Task UpdatePermitField_WhenNotFound_ShouldReturnFalse()
        {
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _ct)).ReturnsAsync((PermitType)null);
            var handler = new UpdatePermitFieldCommandHandler(_mockRepository.Object);

            var result = await handler.Handle(
                new UpdatePermitFieldCommand { PermitTypeId = Guid.NewGuid(), FieldId = Guid.NewGuid(), Name = "X", Type = FieldType.Text, IsRequired = true },
                _ct);

            Assert.False(result);
            _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<PermitType>(), _ct), Times.Never);
        }

        [Fact]
        public async Task RemovePermitField_WhenFound_ShouldRemoveAndReturnTrue()
        {
            var permitType = new PermitType("Building Permit", "Desc", 100m);
            permitType.AddField("FieldA", FieldType.Text, true);
            var fieldId = permitType.Fields[0].Id;
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _ct)).ReturnsAsync(permitType);

            var handler = new RemovePermitFieldCommandHandler(_mockRepository.Object);
            var result = await handler.Handle(new RemovePermitFieldCommand { PermitTypeId = permitType.Id, FieldId = fieldId }, _ct);

            Assert.True(result);
            Assert.Empty(permitType.Fields);
            _mockRepository.Verify(r => r.UpdateAsync(permitType, _ct), Times.Once);
        }

        [Fact]
        public async Task MovePermitField_WhenFound_ShouldReorderAndReturnTrue()
        {
            var permitType = new PermitType("Building Permit", "Desc", 100m);
            permitType.AddField("FieldA", FieldType.Text, true);
            permitType.AddField("FieldB", FieldType.Text, false);
            var aId = permitType.Fields[0].Id;
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _ct)).ReturnsAsync(permitType);

            var handler = new MovePermitFieldCommandHandler(_mockRepository.Object);
            var result = await handler.Handle(new MovePermitFieldCommand { PermitTypeId = permitType.Id, FieldId = aId, NewOrder = 2 }, _ct);

            Assert.True(result);
            Assert.Equal("FieldB", permitType.Fields[0].Name);
            _mockRepository.Verify(r => r.UpdateAsync(permitType, _ct), Times.Once);
        }

        [Fact]
        public async Task UpdateDocumentRequirement_WhenFound_ShouldUpdateAndReturnTrue()
        {
            var permitType = new PermitType("Building Permit", "Desc", 100m);
            permitType.AddDocumentRequirement("ID", true, new[] { ".pdf" }, 1000);
            var reqId = permitType.DocumentRequirements[0].Id;
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _ct)).ReturnsAsync(permitType);

            var handler = new UpdateDocumentRequirementCommandHandler(_mockRepository.Object);
            var result = await handler.Handle(
                new UpdateDocumentRequirementCommand { PermitTypeId = permitType.Id, RequirementId = reqId, IsRequired = false, AllowedExtensions = new[] { ".pdf", ".jpg" }, MaxFileSizeBytes = 5000 },
                _ct);

            Assert.True(result);
            Assert.False(permitType.DocumentRequirements[0].IsRequired);
            _mockRepository.Verify(r => r.UpdateAsync(permitType, _ct), Times.Once);
        }

        [Fact]
        public async Task RemoveDocumentRequirement_WhenFound_ShouldRemoveAndReturnTrue()
        {
            var permitType = new PermitType("Building Permit", "Desc", 100m);
            permitType.AddDocumentRequirement("ID", true, new[] { ".pdf" }, 1000);
            var reqId = permitType.DocumentRequirements[0].Id;
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _ct)).ReturnsAsync(permitType);

            var handler = new RemoveDocumentRequirementCommandHandler(_mockRepository.Object);
            var result = await handler.Handle(new RemoveDocumentRequirementCommand { PermitTypeId = permitType.Id, RequirementId = reqId }, _ct);

            Assert.True(result);
            Assert.Empty(permitType.DocumentRequirements);
            _mockRepository.Verify(r => r.UpdateAsync(permitType, _ct), Times.Once);
        }

        [Fact]
        public async Task MoveDocumentRequirement_WhenFound_ShouldReorderAndReturnTrue()
        {
            var permitType = new PermitType("Building Permit", "Desc", 100m);
            permitType.AddDocumentRequirement("ID", true, new[] { ".pdf" }, 1000);
            permitType.AddDocumentRequirement("Photo", false, new[] { ".png" }, 2000);
            var photoId = permitType.DocumentRequirements[1].Id;
            _mockRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), _ct)).ReturnsAsync(permitType);

            var handler = new MoveDocumentRequirementCommandHandler(_mockRepository.Object);
            var result = await handler.Handle(new MoveDocumentRequirementCommand { PermitTypeId = permitType.Id, RequirementId = photoId, NewOrder = 1 }, _ct);

            Assert.True(result);
            Assert.Equal("Photo", permitType.DocumentRequirements[0].DocumentType);
            _mockRepository.Verify(r => r.UpdateAsync(permitType, _ct), Times.Once);
        }
    }
}
