//----------------------
// TracingBehavior Tests (O3 – Correlation & Distributed Tracing)
//----------------------

using System.Diagnostics;
using ATLAS.Application.Behaviors;
using ATLAS.Application.Commands;
using MediatR;
using Xunit;

namespace ATLAS.Application.Tests.Behaviors
{
    /// <summary>
    /// A minimal concrete command used to exercise TracingBehavior.
    /// </summary>
    public record TestCommand : ICommand<Unit>;

    public class TracingBehaviorTests
    {
        private static TracingBehavior<TestCommand, Unit> CreateBehavior() => new();

        // MediatR 14: RequestHandlerDelegate<T> takes a CancellationToken.
        private static Task<Unit> NextOk(CancellationToken _) => Task.FromResult(Unit.Value);

        [Fact]
        public async Task Handle_CreatesActivity_WhenListenerIsRegistered()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == ATLAS.Application.Telemetry.AtlasTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            var behavior = CreateBehavior();
            var called = false;
            Task<Unit> Next(CancellationToken ct) { called = true; return Task.FromResult(Unit.Value); }

            await behavior.Handle(new TestCommand(), Next, CancellationToken.None);

            Assert.True(called);
        }

        [Fact]
        public async Task Handle_SetsOkStatus_OnSuccess()
        {
            Activity? captured = null;
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == ATLAS.Application.Telemetry.AtlasTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => captured = a
            };
            ActivitySource.AddActivityListener(listener);

            var behavior = CreateBehavior();
            await behavior.Handle(new TestCommand(), NextOk, CancellationToken.None);

            Assert.NotNull(captured);
            Assert.Equal(nameof(TestCommand), captured!.DisplayName);
            Assert.Equal(ActivityStatusCode.Ok, captured.Status);
        }

        [Fact]
        public async Task Handle_SetsErrorStatus_AndRethrows_OnFailure()
        {
            Activity? captured = null;
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == ATLAS.Application.Telemetry.AtlasTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => captured = a
            };
            ActivitySource.AddActivityListener(listener);

            var behavior = CreateBehavior();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                behavior.Handle(new TestCommand(),
                    _ => throw new InvalidOperationException("boom"),
                    CancellationToken.None));

            Assert.NotNull(captured);
            Assert.Equal(ActivityStatusCode.Error, captured!.Status);
        }

        [Fact]
        public async Task Handle_PropagatesAcrossAsyncBoundaries_ActivityRemainsCurrent()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == ATLAS.Application.Telemetry.AtlasTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            var behavior = CreateBehavior();
            Activity? insideNext = null;

            await behavior.Handle(
                new TestCommand(),
                async ct =>
                {
                    // Simulate an asynchronous continuation (e.g. domain event handler).
                    await Task.Yield();
                    insideNext = Activity.Current;
                    return Unit.Value;
                },
                CancellationToken.None);

            // The Activity is current inside the handler's async flow...
            Assert.NotNull(insideNext);

            // ...and stopped afterwards (no leaked ambient Activity).
            Assert.Null(Activity.Current);
        }

        [Fact]
        public async Task Handle_ChildActivity_InheritsParentTraceId()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = _ => true,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            Activity? child = null;
            using var listener2 = new ActivityListener
            {
                ShouldListenTo = source => source.Name == ATLAS.Application.Telemetry.AtlasTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStarted = a => child = a
            };

            var parentSource = new ActivitySource("Test.Parent");
            using var parent = parentSource.StartActivity("ParentOperation");

            ActivitySource.AddActivityListener(listener2);

            var behavior = CreateBehavior();
            await behavior.Handle(new TestCommand(), NextOk, CancellationToken.None);

            Assert.NotNull(parent);
            Assert.NotNull(child);
            // W3C trace context: the command Activity is a child of the existing
            // Activity — same TraceId, different SpanId.
            Assert.Equal(parent!.TraceId, child!.TraceId);
            Assert.NotEqual(parent.SpanId, child.SpanId);
            Assert.Equal(parent.SpanId, child.ParentSpanId);
        }

        [Fact]
        public async Task Handle_NoListener_ProceedsWithoutTracing()
        {
            // No listener registered: StartActivity returns null and the
            // behavior must simply invoke the handler.
            var behavior = CreateBehavior();
            var called = false;
            Task<Unit> Next(CancellationToken ct) { called = true; return Task.FromResult(Unit.Value); }

            var result = await behavior.Handle(new TestCommand(), Next, CancellationToken.None);

            Assert.True(called);
            Assert.Equal(Unit.Value, result);
        }
    }
}