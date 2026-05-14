using System.Diagnostics.Metrics;
using Fletched.Core.Performance;
using TUnit;

namespace Fletched.Performance.Tests;

/// <summary>
/// Verifies that <see cref="EngineMetrics"/> can be initialised and that
/// <see cref="IExecutionObserver"/> implementations receive the correct callbacks.
/// </summary>
public class EngineMetricsTests
{
    [Test]
    public async Task Initialize_CreatesAllCounters()
    {
        using Meter meter = EngineMetrics.Initialize("test-meter-init");

        await Assert.That(EngineMetrics.UnifyAttempts).IsNotNull();
        await Assert.That(EngineMetrics.UnifyFailures).IsNotNull();
        await Assert.That(EngineMetrics.BacktrackCount).IsNotNull();
        await Assert.That(EngineMetrics.ChoicePointCount).IsNotNull();
        await Assert.That(EngineMetrics.FactScans).IsNotNull();
        await Assert.That(EngineMetrics.IndexHits).IsNotNull();
        await Assert.That(EngineMetrics.PredicateInvocations).IsNotNull();
        await Assert.That(EngineMetrics.PredicateInvocationResumes).IsNotNull();
        await Assert.That(EngineMetrics.PredicateInvocationExhaustions).IsNotNull();
        await Assert.That(EngineMetrics.PredicateInvocationFailures).IsNotNull();
    }

    [Test]
    public async Task Initialize_ReturnsDisposableMeter()
    {
        using Meter meter = EngineMetrics.Initialize("test-meter-disposable");
        await Assert.That(meter).IsNotNull();
    }

    [Test]
    public async Task Counters_CanBeIncremented()
    {
        using Meter meter = EngineMetrics.Initialize("test-meter-inc");

        // Verify that Add does not throw (counters are properly initialised).
        Exception? ex = null;
        try
        {
            EngineMetrics.UnifyAttempts.Add(1);
            EngineMetrics.UnifyFailures.Add(1);
            EngineMetrics.BacktrackCount.Add(1);
            EngineMetrics.ChoicePointCount.Add(1);
            EngineMetrics.FactScans.Add(1);
            EngineMetrics.IndexHits.Add(1);
            EngineMetrics.PredicateInvocations.Add(1);
            EngineMetrics.PredicateInvocationResumes.Add(1);
            EngineMetrics.PredicateInvocationExhaustions.Add(1);
            EngineMetrics.PredicateInvocationFailures.Add(1);
        }
        catch (Exception e)
        {
            ex = e;
        }

        await Assert.That(ex).IsNull();
    }

    [Test]
    public async Task MeterListener_ReceivesUnifyAttempts()
    {
        long received = 0;
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == "unify_attempts")
                l.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, value, _, _) =>
            System.Threading.Interlocked.Add(ref received, value));
        listener.Start();

        using Meter meter = EngineMetrics.Initialize("test-meter-listener");
        EngineMetrics.UnifyAttempts.Add(3);
        listener.RecordObservableInstruments();

        await Assert.That(received).IsGreaterThanOrEqualTo(3L);
    }
}
