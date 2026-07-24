//----------------------
// UpdateEmailTemplateCommand — persist edited template content.
// Rejects unknown placeholders and unknown template names.
//----------------------

#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Behaviors;
using FluentValidation.Results;
using ATLAS.Application.Commands;
using ATLAS.Application.EmailTemplates;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Email;
using MediatR;

namespace ATLAS.Application.EmailTemplates.Commands
{
    public class UpdateEmailTemplateCommand : ICommand<bool>
    {
        public UpdateEmailTemplateCommand(string name, string content)
        {
            Name = name;
            Content = content;
        }

        public string Name { get; }
        public string Content { get; }
    }

    public class UpdateEmailTemplateCommandHandler : IRequestHandler<UpdateEmailTemplateCommand, bool>
    {
        private readonly IEmailTemplateStore _store;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public UpdateEmailTemplateCommandHandler(
            IEmailTemplateStore store,
            IMediator mediator,
            ICurrentUserService currentUserService)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<bool> Handle(UpdateEmailTemplateCommand request, CancellationToken cancellationToken)
        {
            var existing = await _store.GetByNameAsync(request.Name, cancellationToken);
            if (existing is null)
                return false;

            var unknown = EmailTemplatePlaceholderValidator
                .GetUnknownPlaceholders(request.Content, KnownEmailPlaceholders.All)
                .ToList();

            if (unknown.Count > 0)
            {
                throw new ValidationException(new List<ValidationFailure>
                {
                    new ValidationFailure("Content",
                        $"Template contains unknown placeholder(s): {string.Join(", ", unknown)}. " +
                        $"Supported placeholders: {string.Join(", ", KnownEmailPlaceholders.All)}.")
                });
            }

            await _store.SaveAsync(new EmailTemplate { Name = request.Name, Content = request.Content }, cancellationToken);

            // Audit: raise a domain event so the existing event-driven audit infrastructure
            // records the change. The handler owns audit creation; this handler stays
            // orchestration-only and never touches audit storage directly.
            await _mediator.Publish(
                new EmailTemplateUpdatedEvent(request.Name),
                cancellationToken);

            return true;
        }
    }
}
