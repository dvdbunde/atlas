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
        public UploadDocumentCommandValidator()
        {
            RuleFor(x => x.ApplicationId)
                .NotEmpty().WithMessage("ApplicationId is required");

            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("FileName is required")
                .MaximumLength(255).WithMessage("FileName cannot exceed 255 characters");

            RuleFor(x => x.ContentType)
                .NotEmpty().WithMessage("ContentType is required")
                .Must(x => x == "application/pdf" || x.StartsWith("image/"))
                .WithMessage("Only PDF and image files are allowed");

            RuleFor(x => x.FileSize)
                .GreaterThan(0).WithMessage("FileSize must be greater than 0")
                .LessThanOrEqualTo(10 * 1024 * 1024).WithMessage("FileSize cannot exceed 10MB");

            RuleFor(x => x.BlobUrl)
                .NotEmpty().WithMessage("BlobUrl is required")
                .Must(x => Uri.TryCreate(x, UriKind.Absolute, out _))
                .WithMessage("BlobUrl must be a valid URL");        
        }
    }
}
