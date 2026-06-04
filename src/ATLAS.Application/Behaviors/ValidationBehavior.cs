using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;

namespace ATLAS.Application.Behaviors
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators ?? throw new ArgumentNullException(nameof(validators));
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!_validators.Any())
            {
                // No validators for this request type, continue
                return await next();
            }

            // Validate the request
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(request, cancellationToken)));

            var failures = validationResults
                .SelectMany(result => result.Errors)
                .Where(error => error != null)
                .ToList();

            if (failures.Any())
            {
                // Throw validation exception
                throw new ValidationException(failures);
            }

            // Validation passed, continue to handler
            return await next();
        }
    }

    public class ValidationException : Exception
    {
        public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
            : base($"Validation failed: {string.Join("; ", failures.Select(f => f.ErrorMessage))}")
        {
            Failures = failures;
        }

        public IEnumerable<FluentValidation.Results.ValidationFailure> Failures { get; }
    }
}
