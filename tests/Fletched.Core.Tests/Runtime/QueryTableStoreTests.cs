using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Core.Tests.Runtime;

public sealed record RuntimeFactRow(string? Parent, string? Child);

public class QueryTableStoreTests
{
    [Test]
    public async Task GetOrAddTable_FirstCallIsProducer_SecondCallIsConsumer()
    {
        var store = new QueryTableStore<(string parent, string child)>();
        TableKey key = TableKey.Create("Ancestor", 2, "Ancestor(parent:alice,child:_)");

        AnswerTable<(string parent, string child)> first = store.GetOrAddTable(key, out bool firstIsProducer);
        AnswerTable<(string parent, string child)> second = store.GetOrAddTable(key, out bool secondIsProducer);

        await Assert.That(firstIsProducer).IsTrue();
        await Assert.That(secondIsProducer).IsFalse();
        await Assert.That(ReferenceEquals(first, second)).IsTrue();
    }

    [Test]
    public async Task TryAddAnswer_DeduplicatesAnswersPerTable()
    {
        var table = new AnswerTable<(string parent, string child)>();

        bool firstInsert = table.TryAddAnswer(("alice", "bob"));
        bool duplicateInsert = table.TryAddAnswer(("alice", "bob"));

        await Assert.That(firstInsert).IsTrue();
        await Assert.That(duplicateInsert).IsFalse();
        await Assert.That(table.Answers.Count).IsEqualTo(1);
    }

    [Test]
    public async Task MarkFaulted_PropagatesStoredException()
    {
        var table = new AnswerTable<(string parent, string child)>();
        var expected = new InvalidOperationException("faulted producer");

        table.MarkFaulted(expected);

        await Assert.That(() => table.ThrowIfFaulted()).Throws<InvalidOperationException>();
        await Assert.That(table.Status).IsEqualTo(TableStatus.Faulted);
    }

    [Test]
    public async Task Clear_RemovesAllTablesFromStore()
    {
        var store = new QueryTableStore<(string parent, string child)>();
        TableKey key = TableKey.Create("Ancestor", 2, "Ancestor(parent:alice,child:_)");
        _ = store.GetOrAddTable(key, out _);

        store.Clear();

        await Assert.That(store.TryGetTable(key, out _)).IsFalse();
    }

    [Test]
    public async Task FactTable_GeneratedAccessorIndex_ReturnsStableFactIndices()
    {
        var table = new FactTable<RuntimeFactRow>(
        [
            new("alice", "bob"),
            new("alice", "carol"),
            new("dave", "erin"),
        ]);

        GeneratedFactIndexAccessor<RuntimeFactRow> accessor = new("Parent", static row => row.Parent);

        bool found = table.TryGetIndex(accessor, "alice", out int[] indices);

        await Assert.That(found).IsTrue();
        await Assert.That(indices).IsEquivalentTo(new[] { 0, 1 });
    }

    [Test]
    public async Task FactTable_NullKeyIndex_UsesDedicatedNullBucket()
    {
        var table = new FactTable<RuntimeFactRow>(
        [
            new(null, "bob"),
            new("alice", "carol"),
            new(null, "erin"),
        ]);

        GeneratedFactIndexAccessor<RuntimeFactRow> accessor = new("Parent", static row => row.Parent);

        bool found = table.TryGetIndex(accessor, key: null, out int[] indices);

        await Assert.That(found).IsTrue();
        await Assert.That(indices).IsEquivalentTo(new[] { 0, 2 });
    }

    [Test]
    public async Task QueryMagicRuntime_SourceScope_IsPerQueryExecution()
    {
        var context = new object();

        using (QueryMagicRuntime.EnterScope(context))
        {
            MagicSource<string> source = QueryMagicRuntime.GetStore<string>(context).GetOrAddSource("Magic_Ancestor_bf");
            _ = source.TryAdd("alice");
            await Assert.That(source.Tuples.Count).IsEqualTo(1);
        }

        using (QueryMagicRuntime.EnterScope(context))
        {
            MagicSource<string> source = QueryMagicRuntime.GetStore<string>(context).GetOrAddSource("Magic_Ancestor_bf");
            await Assert.That(source.Tuples.Count).IsEqualTo(0);
        }
    }

    [Test]
    public async Task QueryMagicRuntime_AndQueryTableRuntime_DoNotShareStorage()
    {
        var context = new object();

        using QueryMagicRuntime.QueryScope magicScope = QueryMagicRuntime.EnterScope(context);
        using QueryTableRuntime.QueryScope tableScope = QueryTableRuntime.EnterScope(context);

        MagicSource<string> magicSource = QueryMagicRuntime.GetStore<string>(context).GetOrAddSource("Magic_Ancestor_bf");
        _ = magicSource.TryAdd("alice");

        QueryTableStore<(string parent, string child)> tableStore = QueryTableRuntime.GetStore<(string parent, string child)>(context);
        AnswerTable<(string parent, string child)> answerTable = tableStore.GetOrAddTable(
            TableKey.Create("Ancestor", 2, "b:alice|f"),
            out bool isProducer);

        await Assert.That(isProducer).IsTrue();
        await Assert.That(magicSource.Tuples.Count).IsEqualTo(1);
        await Assert.That(answerTable.Answers.Count).IsEqualTo(0);
    }
}
