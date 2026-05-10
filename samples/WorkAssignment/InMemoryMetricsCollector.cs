using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace WorkAssignment;

public sealed class InMemoryMetricsCollector : IDisposable
{
    private readonly MeterListener _listener;
    private readonly ConcurrentDictionary<string, long> _values = new(StringComparer.Ordinal);

    public InMemoryMetricsCollector(string meterName)
    {
        _listener = new MeterListener();
        _listener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == meterName)
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        _listener.SetMeasurementEventCallback<long>((instrument, measurement, _, _) =>
        {
            _values.AddOrUpdate(instrument.Name, measurement, (_, existing) => existing + measurement);
        });

        _listener.Start();
    }

    public IReadOnlyDictionary<string, long> Snapshot() =>
        _values.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.Ordinal);

    public void Dispose()
    {
        _listener.Dispose();
    }
}
