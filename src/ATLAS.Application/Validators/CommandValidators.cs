using ATLAS.Application.Commands.Applications;
using ATLAS.Application.Commands.Documents;
using ATLAS.Application.Commands.PermitTypes;
using FluentValidation;
using System;

namespace ATLAS.Application.Commands.Validators
{
    public class ApproveApplicationCommandValidator : AbstractValidator<ApproveApplicationCommand>
    {
        public ApproveApplicationCommandValidator()
        {
            RuleFor(x => x.ApplicationId)
                .NotEmpty().WithMessage("ApplicationId is required");

            RuleFor(x => x.Comments)
                .MaximumLength(2000).WithMessage("Comments cannot exceed 2000 characters");
        }
    }

    public class RejectApplicationCommandValidator : AbstractValidator<RejectApplicationCommand>
    {
        public RejectApplicationCommandValidator()
        {
            RuleFor(x => x.ApplicationId)
                .NotEmpty().WithMessage("ApplicationId is required");

            RuleFor(x => x.ReasonCode)
                .NotEmpty().WithMessage("Rejection reason code is required")
                .MaximumLength(1000).WithMessage("Rejection reason code cannot exceed 1000 characters");
        }
    }

    public class RequestInfoCommandValidator : AbstractValidator<RequestInfoCommand>
    {
        public RequestInfoCommandValidator()
        {
            RuleFor(x => x.ApplicationId)
                .NotEmpty().WithMessage("ApplicationId is required");

            RuleFor(x => x.Message)
                .NotEmpty().WithMessage("Message is required")
                .MaximumLength(2000).WithMessage("Message cannot exceed 2000 characters");
        }
    }

    public class AssignApplicationToMeCommandValidator : AbstractValidator<AssignApplicationToMeCommand>
    {
        public AssignApplicationToMeCommandValidator()
        {
            RuleFor(x => x.ApplicationId)
                .NotEmpty().WithMessage("ApplicationId is required");            
        }
    }

    public class UpdatePermitTypeCommandValidator : AbstractValidator<UpdatePermitTypeCommand>
    {
        public UpdatePermitTypeCommandValidator()
        {
            RuleFor(x => x.PermitTypeId)
                .NotEmpty().WithMessage("PermitTypeId is required");

            RuleFor(x => x.Fee)
                .GreaterThanOrEqualTo(0).WithMessage("Fee cannot be negative");
        }
    }

    public class UpdatePermitTypeGeneralInformationCommandValidator : AbstractValidator<UpdatePermitTypeGeneralInformationCommand>
    {
        public UpdatePermitTypeGeneralInformationCommandValidator()
        {
            RuleFor(x => x.PermitTypeId)
                .NotEmpty().WithMessage("PermitTypeId is required");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MinimumLength(3).WithMessage("Name must be at least 3 characters")
                .MaximumLength(100).WithMessage("Name must be at most 100 characters");
        }
    }

    public class DeactivatePermitTypeCommandValidator : AbstractValidator<DeactivatePermitTypeCommand>
    {
        public DeactivatePermitTypeCommandValidator()
        {
            RuleFor(x => x.PermitTypeId)
                .NotEmpty().WithMessage("PermitTypeId is required");
        }
    }

    public class ActivatePermitTypeCommandValidator : AbstractValidator<ActivatePermitTypeCommand>
    {
        public ActivatePermitTypeCommandValidator()
        {
            RuleFor(x => x.PermitTypeId)
                .NotEmpty().WithMessage("PermitTypeId is required");
        }
    }

    public class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
    {
        // Allowed MIME types per PRD F-03
        private static readonly string[] AllowedContentTypes =
        {
            "application/pdf",
            "image/jpeg",
            "image/png"
        };

        // Maximum file size: 25MB (aligned with Document entity as source of truth)
        private const long MaxFileSizeBytes = 25 * 1024 * 1024;

        public UploadDocumentCommandValidator()
        {
            RuleFor(x => x.ApplicationId)
                .NotEmpty().WithMessage("Application ID is required.");

            RuleFor(x => x.FileContent)
                .NotNull().WithMessage("File content is required.")
                .Must(s => s != Stream.Null).WithMessage("File content stream cannot be null.");

            RuleFor(x => x.FileContent)
                .NotNull().WithMessage("File content is required.")
                .Must(s => s != Stream.Null).WithMessage("File content stream cannot be null.");

            RuleFor(x => x.DocumentType)
                .NotEmpty().WithMessage("Document type is required.")
                .MaximumLength(100).WithMessage("Document type cannot exceed 100 characters.");

            RuleFor(x => x.FileSize)
                .GreaterThan(0).WithMessage("File size must be positive.")
                .LessThanOrEqualTo(MaxFileSizeBytes)
                    .WithMessage($"File size cannot exceed {MaxFileSizeBytes / (1024 * 1024)}MB.");

            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("File name is required.")
                .MaximumLength(255).WithMessage("File name cannot exceed 255 characters.")
                .Must(name => !string.IsNullOrWhiteSpace(Path.GetExtension(name)))
                    .WithMessage("File must have an extension.");

            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("Content type is required.")
                .Must(ct => AllowedContentTypes.Contains(ct.ToLowerInvariant()))
                    .WithMessage($"Content type must be one of: {string.Join(", ", AllowedContentTypes)}.");

            RuleFor(x => x.FileSize)
                .GreaterThan(0).WithMessage("File size must be positive.")
                .LessThanOrEqualTo(MaxFileSizeBytes)
                    .WithMessage($"File size cannot exceed {MaxFileSizeBytes / (1024 * 1024)}MB.");
        }
    }

    public class UpdatePermitFieldCommandValidator : AbstractValidator<UpdatePermitFieldCommand>
    {
        public UpdatePermitFieldCommandValidator()
        {
            RuleFor(x => x.PermitTypeId).NotEmpty().WithMessage("PermitTypeId is required");
            RuleFor(x => x.FieldId).NotEmpty().WithMessage("FieldId is required");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Field name is required")
                .MaximumLength(100).WithMessage("Field name cannot exceed 100 characters");
        }
    }

    public class AddPermitFieldCommandValidator : AbstractValidator<AddPermitFieldCommand>
    {
        public AddPermitFieldCommandValidator()
        {
            RuleFor(x => x.PermitTypeId).NotEmpty().WithMessage("PermitTypeId is required");
            RuleFor(x => x.Name).NotEmpty().WithMessage("Field name is required")
                .MinimumLength(2).WithMessage("Field name must be at least 2 characters")
                .MaximumLength(100).WithMessage("Field name cannot exceed 100 characters");
            RuleFor(x => x.Type).IsInEnum().WithMessage("A valid field type is required");
            RuleFor(x => x.Options)
                .Must(options => options == null || options.Count == 0 || options.All(o => !string.IsNullOrWhiteSpace(o)))
                .WithMessage("Dropdown options must not be empty");
        }
    }

    public class RemovePermitFieldCommandValidator : AbstractValidator<RemovePermitFieldCommand>
    {
        public RemovePermitFieldCommandValidator()
        {
            RuleFor(x => x.PermitTypeId).NotEmpty().WithMessage("PermitTypeId is required");
            RuleFor(x => x.FieldId).NotEmpty().WithMessage("FieldId is required");
        }
    }

    public class MovePermitFieldCommandValidator : AbstractValidator<MovePermitFieldCommand>
    {
        public MovePermitFieldCommandValidator()
        {
            RuleFor(x => x.PermitTypeId).NotEmpty().WithMessage("PermitTypeId is required");
            RuleFor(x => x.FieldId).NotEmpty().WithMessage("FieldId is required");
            RuleFor(x => x.NewOrder).GreaterThan(0).WithMessage("NewOrder must be greater than 0");
        }
    }

    public class UpdateDocumentRequirementCommandValidator : AbstractValidator<UpdateDocumentRequirementCommand>
    {
        public UpdateDocumentRequirementCommandValidator()
        {
            RuleFor(x => x.PermitTypeId).NotEmpty().WithMessage("PermitTypeId is required");
            RuleFor(x => x.RequirementId).NotEmpty().WithMessage("RequirementId is required");
            RuleFor(x => x.AllowedExtensions).NotNull().WithMessage("AllowedExtensions is required")
                .Must(e => e.Length > 0).WithMessage("At least one allowed extension is required");
            RuleFor(x => x.MaxFileSizeBytes).GreaterThan(0).WithMessage("MaxFileSizeBytes must be positive");
        }
    }

    public class AddDocumentRequirementCommandValidator : AbstractValidator<AddDocumentRequirementCommand>
    {
        public AddDocumentRequirementCommandValidator()
        {
            RuleFor(x => x.PermitTypeId).NotEmpty().WithMessage("PermitTypeId is required");
            RuleFor(x => x.DocumentType).NotEmpty().WithMessage("Document type is required")
                .MaximumLength(100).WithMessage("Document type cannot exceed 100 characters");
            RuleFor(x => x.AllowedExtensions).NotNull().WithMessage("AllowedExtensions is required")
                .Must(e => e.Length > 0).WithMessage("At least one allowed extension is required");
            RuleFor(x => x.MaxFileSizeBytes).GreaterThan(0).WithMessage("MaxFileSizeBytes must be positive");
        }
    }

    public class RemoveDocumentRequirementCommandValidator : AbstractValidator<RemoveDocumentRequirementCommand>
    {
        public RemoveDocumentRequirementCommandValidator()
        {
            RuleFor(x => x.PermitTypeId).NotEmpty().WithMessage("PermitTypeId is required");
            RuleFor(x => x.RequirementId).NotEmpty().WithMessage("RequirementId is required");
        }
    }

    public class MoveDocumentRequirementCommandValidator : AbstractValidator<MoveDocumentRequirementCommand>
    {
        public MoveDocumentRequirementCommandValidator()
        {
            RuleFor(x => x.PermitTypeId).NotEmpty().WithMessage("PermitTypeId is required");
            RuleFor(x => x.RequirementId).NotEmpty().WithMessage("RequirementId is required");
            RuleFor(x => x.NewOrder).GreaterThan(0).WithMessage("NewOrder must be greater than 0");
        }
    }

    public class CreatePermitTypeCommandValidator : AbstractValidator<CreatePermitTypeCommand>
    {
        public CreatePermitTypeCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required")
                .MinimumLength(3).WithMessage("Name must be at least 3 characters")
                .MaximumLength(100).WithMessage("Name must be at most 100 characters");

            RuleFor(x => x.Fee)
                .GreaterThanOrEqualTo(0).WithMessage("Fee cannot be negative");
        }
    }
}
