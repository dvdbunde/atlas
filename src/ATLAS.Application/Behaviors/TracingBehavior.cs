//----------------------
// Command Tracing Behavior (O3 – Correlation & Distributed Tracing)
// MediatR pipeline behavior that opens a W3C Activity for every application
// command. This is the operation boundary for Blazor-initiated operations:
//
//   Blazor UI action → MediatR command → [this Activity] → handlers → dependencies
//
// Uses System.Diagnostics.ActivitySource (W3C trace context) so Application
// Insights collects it as a request/operation and child dependency telemetry
// (EF Core, Azure SDK) automatically associates with it via Activity.Current.
//----------------------

#nullable enable

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ATLAS.Application.Commands;
using MediatR;

namespace ATLAS.Application.Behaviors
{
    /// <summary>
    /// Opens a diagnostic Activity around each command execution.
    ///
    /// Design decisions:
    /// - Commands only: queries are reads without side effects and would double
    ///   the telemetry volume for little diagnostic value.
    /// - Activity names are the stable command type name (low cardinality);
    ///   dynamic values such as ApplicationId go into structured log properties,
    ///   never the Activity name.
    /// - If a parent Activity already exists (e.g. framework-created), this
    ///   Activity becomes its child — never a second root.
    /// - The ActivitySource name is defined once in
    ///   <see cref="ATLAS.Application.Telemetry.AtlasTelemetry.ActivitySourceName"/>
    ///   and registered by host applications via OpenTelemetry AddSource.
    /// </summary>
    public class TracingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        private static readonly ActivitySource Source = new(ATLAS.Application.Telemetry.AtlasTelemetry.ActivitySourceName);

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Only commands get an operation boundary; queries pass through.
            if (request is not ICommand<TResponse>)
            {
                return await next();
            }

            var activityName = request.GetType().Name;

            using var activity = Source.StartActivity(activityName, ActivityKind.Internal);
            if (activity is null)
            {
                // No listener registered (e.g. local dev without telemetry):
                // proceed without tracing.
                return await next();
            }

            // O4: command duration metric. The Activity name is the stable command
            // type name - bounded cardinality (one dimension value per command
            // type), never IDs or user data.
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var response = await next();
                activity.SetStatus(ActivityStatusCode.Ok);
                ATLAS.Application.Telemetry.AtlasMetrics.CommandDuration.Record(
                    stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("command", activityName));
                return response;
            }
            catch (Exception ex)
            {
                activity.SetStatus(ActivityStatusCode.Error, ex.Message);
                activity.AddException(ex);
                ATLAS.Application.Telemetry.AtlasMetrics.CommandDuration.Record(
                    stopwatch.ElapsedMilliseconds, new KeyValuePair<string, object?>("command", activityName));
                throw;
            }
        }
    }
}