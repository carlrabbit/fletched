using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Core.Tests.Runtime;

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
}
