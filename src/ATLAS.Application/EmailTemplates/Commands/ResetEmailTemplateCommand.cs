//----------------------
// ResetEmailTemplateCommand — reset a customized template to its source-code default.
// Deletes the customized version (Blob Storage) without touching the default.
//----------------------

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Behaviors;
using ATLAS.Application.Commands;
using ATLAS.Application.EmailTemplates;
using ATLAS.Application.Interfaces;
using ATLAS.Domain.Email;
using MediatR;

namespace ATLAS.Application.EmailTemplates.Commands
{
    public class ResetEmailTemplateCommand : ICommand<bool>
    {
        public ResetEmailTemplateCommand(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public class ResetEmailTemplateCommandHandler : IRequestHandler<ResetEmailTemplateCommand, bool>
    {
        private readonly IEmailTemplateStore _store;
        private readonly IMediator _mediator;
        private readonly ICurrentUserService _currentUserService;

        public ResetEmailTemplateCommandHandler(
            IEmailTemplateStore store,
            IMediator mediator,
            ICurrentUserService currentUserService)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        }

        public async Task<bool> Handle(ResetEmailTemplateCommand request, CancellationToken cancellationToken)
        {
            var existing = await _store.GetByNameAsync(request.Name, cancellationToken);
            if (existing is null)
                return false;

            await _store.ResetAsync(request.Name, cancellationToken);

            // Audit: raise a domain event so the existing event-driven audit
            // infrastructure records the change.
            await _mediator.Publish(
                new EmailTemplateResetEvent(request.Name),
                cancellationToken);

            return true;
        }
    }
}
