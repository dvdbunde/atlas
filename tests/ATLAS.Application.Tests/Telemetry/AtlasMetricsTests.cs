//----------------------
// AtlasMetrics Tests (O4 - Metrics & Telemetry)
// Verify metric semantics using an in-memory MeterListener - no Azure
// dependency. Tests assert the metric contract: names, dimensions, and
// exactly-once emission at the defined boundaries.
//----------------------

using System.Diagnostics;
using System.Diagnostics.Metrics;
using ATLAS.Application.Behaviors;
using ATLAS.Application.Commands;
using ATLAS.Application.Telemetry;
using MediatR;
using Xunit;

namespace ATLAS.Application.Tests.Telemetry
{
    /// <summary>
    /// A minimal concrete command used to exercise TracingBehavior metrics.
    /// </summary>
    public record MetricsTestCommand : ICommand<Unit>;

    public class AtlasMetricsTests : IDisposable
    {
        private readonly MeterListener _listener = new();
        private readonly Dictionary<string, (double Value, KeyValuePair<string, object?>[] Tags)> _measurements = new();

        public AtlasMetricsTests()
        {
            _listener.InstrumentPublished = (instrument, listener2) =>
            {
                if (instrument.Meter.Name == AtlasMetrics.MeterName)
                {
                    listener2.EnableMeasurementEvents(instrument);
                }
            };
            _listener.SetMeasurementEventCallback<long>((inst, measurement, tags, _) =>
                RecordMeasurement(inst.Name, measurement, tags.ToArray()));
            _listener.SetMeasurementEventCallback<double>((inst, measurement, tags, _) =>
                RecordMeasurement(inst.Name, measurement, tags.ToArray()));
            _listener.Start();
        }

        public void Dispose() => _listener.Dispose();

        private void RecordMeasurement(string name, double value, KeyValuePair<string, object?>[] tags)
            => _measurements[name] = (value, tags);

        [Fact]
        public void CommandDuration_Recorded_OnSuccessfulCommand()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == AtlasTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            var behavior = new TracingBehavior<MetricsTestCommand, Unit>();
            behavior.Handle(new MetricsTestCommand(), NextOk, CancellationToken.None).GetAwaiter().GetResult();

            Assert.True(_measurements.ContainsKey("atlas.command.duration"));
            var (value, tags) = _measurements["atlas.command.duration"];
            Assert.True(value >= 0);
            Assert.Contains(tags, t => t.Key == "command" && (string?)t.Value == nameof(MetricsTestCommand));
        }

        [Fact]
        public void CommandDuration_Recorded_OnFailingCommand()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == AtlasTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            var behavior = new TracingBehavior<MetricsTestCommand, Unit>();

            Assert.ThrowsAny<Exception>(() =>
                behavior.Handle(new MetricsTestCommand(),
                    _ => throw new InvalidOperationException("boom"),
                    CancellationToken.None).GetAwaiter().GetResult());

            Assert.True(_measurements.ContainsKey("atlas.command.duration"));
        }

        [Fact]
        public void CommandDimensions_AreLowCardinality_CommandNameOnly()
        {
            using var listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == AtlasTelemetry.ActivitySourceName,
                Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(listener);

            var behavior = new TracingBehavior<MetricsTestCommand, Unit>();
            behavior.Handle(new MetricsTestCommand(), NextOk, CancellationToken.None).GetAwaiter().GetResult();

            var (_, tags) = _measurements["atlas.command.duration"];
            // Exactly one dimension ("command") - no IDs or user data.
            Assert.Single(tags);
            Assert.Equal("command", tags[0].Key);
        }

        [Fact]
        public void ApplicationTransitions_Instrument_IsCounterWithExpectedName()
        {
            Assert.Equal("atlas.applications.transitions", AtlasMetrics.ApplicationTransitions.Name);
            Assert.IsType<Counter<long>>(AtlasMetrics.ApplicationTransitions);
        }

        [Fact]
        public void EmailSends_Instrument_IsCounterWithExpectedName()
        {
            Assert.Equal("atlas.email.sends", AtlasMetrics.EmailSends.Name);
            Assert.IsType<Counter<long>>(AtlasMetrics.EmailSends);
        }

        [Fact]
        public void EmailDuration_Instrument_IsHistogramWithExpectedName()
        {
            Assert.Equal("atlas.email.duration", AtlasMetrics.EmailDuration.Name);
            Assert.IsType<Histogram<double>>(AtlasMetrics.EmailDuration);
        }

        [Fact]
        public void CommandDuration_Instrument_IsHistogramWithExpectedName()
        {
            Assert.Equal("atlas.command.duration", AtlasMetrics.CommandDuration.Name);
            Assert.IsType<Histogram<double>>(AtlasMetrics.CommandDuration);
        }

        [Fact]
        public void MeterName_MatchesActivitySourceName()
        {
            // One meter/source identity for both O3 tracing and O4 metrics.
            Assert.Equal(AtlasTelemetry.ActivitySourceName, AtlasMetrics.MeterName);
        }

        private static Task<Unit> NextOk(CancellationToken _) => Task.FromResult(Unit.Value);
    }
}
