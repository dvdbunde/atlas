//----------------------
// GetEmailTemplatesQuery — list all application-owned email templates.
//----------------------

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.EmailTemplates;
using MediatR;

namespace ATLAS.Application.EmailTemplates.Queries
{
    public class GetEmailTemplatesQuery : IRequest<IReadOnlyList<EmailTemplate>>
    {
    }

    public class GetEmailTemplatesQueryHandler
        : IRequestHandler<GetEmailTemplatesQuery, IReadOnlyList<EmailTemplate>>
    {
        private readonly IEmailTemplateStore _store;

        public GetEmailTemplatesQueryHandler(IEmailTemplateStore store)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public async Task<IReadOnlyList<EmailTemplate>> Handle(
            GetEmailTemplatesQuery request, CancellationToken cancellationToken)
        {
            var names = await _store.GetTemplateNamesAsync(cancellationToken);
            var result = new List<EmailTemplate>();

            foreach (var name in names)
            {
                var template = await _store.GetByNameAsync(name, cancellationToken);
                if (template is not null)
                    result.Add(template);
            }

            return result;
        }
    }
}
