using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Commands;
using ATLAS.Domain.Interfaces;
using MediatR;

namespace ATLAS.Application.Behaviors
{
    /// <summary>
    /// MediatR pipeline behavior that commits the unit of work after every
    /// successful command execution.
    ///
    /// Pipeline position: runs AFTER UserSynchronizationBehavior and BEFORE
    /// the command handler. Registered last so it wraps the handler call
    /// (onion model), allowing it to call next() then SaveChangesAsync().
    ///
    /// Constraint: only activates for types implementing ICommand<TResponse>,
    /// so queries and other IRequest types are not affected.
    ///
    /// Design decisions:
    /// - Calls SaveChangesAsync() only if next() succeeds — failed handlers
    ///   never produce a partial commit.
    /// - Does NOT catch or wrap exceptions — failures propagate to the caller.
    /// - Does NOT create an EF Core IDbContextTransaction — the implicit
    ///   transaction from SaveChangesAsync() is sufficient for single-command
    ///   scenarios. A distributed transaction coordinator would be needed only
    ///   if a single command writes to multiple independent stores.
    /// - The UserSynchronizationBehavior commits independently before
    ///   TransactionBehavior runs — see IdentityResolver for that boundary.
    /// </summary>
    public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : ICommand<TResponse>
    {
        private readonly IUnitOfWork _unitOfWork;

        public TransactionBehavior(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Execute the command handler (and any inner pipeline behaviors)
            var response = await next();

            // Commit all tracked changes made by the handler
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return response;
        }
    }
}