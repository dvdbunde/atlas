using ATLAS.Application.Commands.PermitTypes;
using ATLAS.Application.Commands.Validators;
using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

namespace ATLAS.Application.Tests.Validators
{
    public class PermitTypeCommandValidatorsTests
    {
        private readonly UpdatePermitTypeCommandValidator _updateValidator = new();
        private readonly DeactivatePermitTypeCommandValidator _deactivateValidator = new();

        [Fact]
        public void Update_WithEmptyPermitTypeId_ShouldHaveValidationError()
        {
            var command = new UpdatePermitTypeCommand { PermitTypeId = Guid.Empty };
            var result = _updateValidator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.PermitTypeId);
        }

        [Fact]
        public void Update_WithNegativeFee_ShouldHaveValidationError()
        {
            var command = new UpdatePermitTypeCommand { PermitTypeId = Guid.NewGuid(), Fee = -1m };
            var result = _updateValidator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.Fee);
        }

        [Fact]
        public void Update_WithValidCommand_ShouldNotHaveValidationError()
        {
            var command = new UpdatePermitTypeCommand { PermitTypeId = Guid.NewGuid(), Fee = 10m, IsActive = true };
            var result = _updateValidator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Deactivate_WithEmptyPermitTypeId_ShouldHaveValidationError()
        {
            var command = new DeactivatePermitTypeCommand { PermitTypeId = Guid.Empty, DeactivatedByAdminId = Guid.NewGuid() };
            var result = _deactivateValidator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.PermitTypeId);
        }

        [Fact]
        public void Deactivate_WithEmptyAdminId_ShouldHaveValidationError()
        {
            var command = new DeactivatePermitTypeCommand { PermitTypeId = Guid.NewGuid(), DeactivatedByAdminId = Guid.Empty };
            var result = _deactivateValidator.TestValidate(command);
            result.ShouldHaveValidationErrorFor(x => x.DeactivatedByAdminId);
        }

        [Fact]
        public void Deactivate_WithValidCommand_ShouldNotHaveValidationError()
        {
            var command = new DeactivatePermitTypeCommand { PermitTypeId = Guid.NewGuid(), DeactivatedByAdminId = Guid.NewGuid() };
            var result = _deactivateValidator.TestValidate(command);
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}
