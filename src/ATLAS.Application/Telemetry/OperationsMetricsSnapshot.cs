//----------------------
// Operations Metrics Snapshot (O5 – Operations Portal)
// Reads the existing O4 metric instruments (atlas.*) via a MeterListener to
// expose process-lifetime aggregates for the Administration Operations page.
//
// This does NOT create new telemetry — it observes the instruments already
// emitted by O4. Values are cumulative since process start; time-windowed
// analysis remains an Azure Monitor/Grafana concern (O6).
//----------------------

using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace ATLAS.Application.Telemetry
{
    /// <summary>
    /// Read-only snapshot of ATLAS O4 metric counters for the Operations Portal.
    /// Registered as a singleton; listens to the ATLAS meter for the lifetime of
    /// the process. Values are cumulative since application start.
    /// </summary>
    public interface IOperationsMetricsSnapshot
    {
        /// <summary>Cumulative count by transition (created, submitted, ...).</summary>
        IReadOnlyDictionary<string, long> ApplicationTransitions { get; }

        /// <summary>Cumulative email sends by outcome (success, failure).</summary>
        IReadOnlyDictionary<string, long> EmailSends { get; }

        /// <summary>True once at least one measurement has been received for the meter.</summary>
        bool HasData { get; }
    }

    public class OperationsMetricsSnapshot : IOperationsMetricsSnapshot, IDisposable
    {
        private readonly MeterListener _listener;
        private readonly ConcurrentDictionary<string, long> _transitions = new();
        private readonly ConcurrentDictionary<string, long> _emailSends = new();
        private int _hasData;

        public OperationsMetricsSnapshot()
        {
            _listener = new MeterListener
            {
                InstrumentPublished = (instrument, enable) =>
                {
                    // Explicit parentheses: the instrument must belong to the ATLAS
                    // meter AND be one of the two observed instruments. Without
                    // grouping, && binds tighter than || and an instrument named
                    // atlas.email.sends from ANY meter would be accepted.
                    if (instrument.Meter.Name == AtlasMetrics.MeterName &&
                        (instrument.Name == AtlasMetrics.ApplicationTransitionsName ||
                         instrument.Name == AtlasMetrics.EmailSendsName))
                    {
                        enable.EnableMeasurementEvents(instrument);
                    }
                }
            };

            _listener.SetMeasurementEventCallback<long>(
                (instrument, measurement, tags, state) =>
                    OnMeasurement(instrument.Name, measurement, tags, state));
            _listener.Start();
        }

        private void OnMeasurement(string name, long value, ReadOnlySpan<KeyValuePair<string, object?>> tags, object? state)
        {
            Interlocked.Exchange(ref _hasData, 1);

            var outcomeOrTransition = "unknown";
            foreach (var tag in tags)
            {
                if (tag.Key is "transition" or "outcome" && tag.Value is string s)
                {
                    outcomeOrTransition = s;
                    break;
                }
            }

            if (name == AtlasMetrics.ApplicationTransitionsName)
            {
                _transitions.AddOrUpdate(outcomeOrTransition, value, (_, existing) => existing + value);
            }
            else if (name == AtlasMetrics.EmailSendsName)
            {
                _emailSends.AddOrUpdate(outcomeOrTransition, value, (_, existing) => existing + value);
            }
        }

        public IReadOnlyDictionary<string, long> ApplicationTransitions => _transitions;
        public IReadOnlyDictionary<string, long> EmailSends => _emailSends;
        public bool HasData => Volatile.Read(ref _hasData) == 1;

        public void Dispose() => _listener.Dispose();
    }
}