namespace ATLAS.Application.Commands
{
    /// <summary>
    /// Marker interface for CQRS commands (write operations).
    /// Extends MediatR's IRequest so that command classes can be used
    /// interchangeably with IRequest in MediatR pipelines and handlers.
    ///
    /// This enables the TransactionBehavior to constrain itself to
    /// command types only, ensuring queries never trigger a commit.
    /// </summary>
    /// <typeparam name="TResponse">The response type returned by the command handler.</typeparam>
    public interface ICommand<TResponse> : MediatR.IRequest<TResponse>
    {
    }
}