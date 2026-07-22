//----------------------
// Email Template Validators (auto-discovered via AddValidatorsFromAssembly)
//----------------------

#nullable enable

using ATLAS.Application.EmailTemplates.Commands;
using ATLAS.Application.EmailTemplates.Queries;
using FluentValidation;

namespace ATLAS.Application.EmailTemplates.Validators
{
    public class GetEmailTemplateByNameQueryValidator : AbstractValidator<GetEmailTemplateByNameQuery>
    {
        public GetEmailTemplateByNameQueryValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Template name is required");
        }
    }

    public class UpdateEmailTemplateCommandValidator : AbstractValidator<UpdateEmailTemplateCommand>
    {
        public UpdateEmailTemplateCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Template name is required");

            RuleFor(x => x.Content)
                .NotNull().WithMessage("Template content is required");
        }
    }

    public class PreviewEmailTemplateQueryValidator : AbstractValidator<PreviewEmailTemplateQuery>
    {
        public PreviewEmailTemplateQueryValidator()
        {
            RuleFor(x => x.Content)
                .NotNull().WithMessage("Template content is required");
        }
    }
}
