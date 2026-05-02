using Fletched.Core;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Features.Tests;

// ── Sample [Fact] type ───────────────────────────────────────────────────────

[Fact]
public partial record struct Person(string Login, string Name);

// ── Sample [Predicate] type ──────────────────────────────────────────────────

[Predicate]
public partial record struct PersonNames
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<Person>(person => person.Name == name);
}

// ── Tests ────────────────────────────────────────────────────────────────────

public class GeneratorSmokeTests
{
    [Test]
    public async Task Generator_IsReferenced()
    {
        // The generator is referenced as an analyzer — if it compiled, we're good.
        await Assert.That(1 + 1).IsEqualTo(2);
    }

    [Test]
    public async Task FactType_GeneratesEngineContextProperty()
    {
        // The generator adds a Persons property to EngineContext
        var ctx = new EngineContext();
        ctx.Persons = new FactTable<Person>(new[]
        {
            new Person("alice", "Alice"),
            new Person("bob",   "Bob"),
        });
        await Assert.That(ctx.Persons.Data.Length).IsEqualTo(2);
    }

    [Test]
    public async Task Predicate_Execute_ReturnsAllNames()
    {
        var ctx = new EngineContext();
        ctx.Persons = new FactTable<Person>(new[]
        {
            new Person("alice", "Alice"),
            new Person("bob",   "Bob"),
        });

        System.Collections.Generic.List<PersonNamesResult> results =
            default(PersonNames).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(2);
        await Assert.That(results[0].name).IsEqualTo("Alice");
        await Assert.That(results[1].name).IsEqualTo("Bob");
    }
}
