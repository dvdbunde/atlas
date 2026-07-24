//----------------------
// GetEmailTemplateByNameQuery — fetch a single template by name.
// Returns null when the name is unknown or the template file is missing.
//----------------------

#nullable enable

using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.EmailTemplates;
using MediatR;

namespace ATLAS.Application.EmailTemplates.Queries
{
    public class GetEmailTemplateByNameQuery : IRequest<EmailTemplate?>
    {
        public GetEmailTemplateByNameQuery(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public class GetEmailTemplateByNameQueryHandler
        : IRequestHandler<GetEmailTemplateByNameQuery, EmailTemplate?>
    {
        private readonly IEmailTemplateStore _store;

        public GetEmailTemplateByNameQueryHandler(IEmailTemplateStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public Task<EmailTemplate?> Handle(
            GetEmailTemplateByNameQuery request, CancellationToken cancellationToken)
        {
            return _store.GetByNameAsync(request.Name, cancellationToken);
        }
    }
}
