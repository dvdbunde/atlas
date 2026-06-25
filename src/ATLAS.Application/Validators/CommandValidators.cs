using FluentValidation;
using System;

namespace ATLAS.Application.Commands
{
    public class SubmitApplicationCommandValidator : AbstractValidator<SubmitApplicationCommand>
    {
        public SubmitApplicationCommandValidator()
        {          
            RuleFor(x => x.PermitTypeId)
                .NotEmpty().WithMessage("PermitTypeId is required");

            RuleFor(x => x.CitizenNotes)
                .MaximumLength(2000).WithMessage("Citizen notes cannot exceed 2000 characters");
        }
    }

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

    public class AssignToOfficerCommandValidator : AbstractValidator<AssignToOfficerCommand>
    {
        public AssignToOfficerCommandValidator()
        {
            RuleFor(x => x.ApplicationId)
                .NotEmpty().WithMessage("ApplicationId is required");

            RuleFor(x => x.OfficerId)
                .NotEmpty().WithMessage("OfficerId is required");
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
}
