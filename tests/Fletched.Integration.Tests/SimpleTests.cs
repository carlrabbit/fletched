using System.Collections.Generic;
using System.Linq;
using Fletched.Core;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Integration.Tests;

// ── Domain model ─────────────────────────────────────────────────────────────

[Fact]
public partial record struct User(string Login, string Name, bool IsAdmin);

// ── Predicates ────────────────────────────────────────────────────────────────

/// <summary>Returns the name of every user.</summary>
[Predicate]
public partial record struct UserNames
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<User>(u => u.Name == name);
}

/// <summary>Returns the login of every admin user.</summary>
[Predicate]
public partial record struct AdminLogins
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> login) =>
        Logic.With<User>(u => u.Login == login && u.IsAdmin == true);
}

[Predicate]
public partial record struct MatchingUserNames
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> login, TerminalVar<string> name) =>
        Logic.With<User>(first =>
            first.Login == login &&
            Logic.With<User>(second => second.Login == login && second.Name == name));
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class SimpleTests
{
    private static EngineContext BuildContext()
    {
        var ctx = new EngineContext();
        ctx.Users = new FactTable<User>(new[]
        {
            new User("alice", "Alice", true),
            new User("bob",   "Bob",   false),
            new User("carol", "Carol", true),
        });
        return ctx;
    }

    [Test]
    public async Task Placeholder_AlwaysPasses()
    {
        await Assert.That(BuildContext().Users.Data.Length).IsEqualTo(3);
    }

    [Test]
    public async Task UserNames_ReturnsAllUsers()
    {
        EngineContext ctx = BuildContext();
        System.Collections.Generic.List<UserNamesResult> results =
            await default(UserNames).ExecuteAsync(ctx).ToListAsync();

        await Assert.That(results.Count).IsEqualTo(3);
    }

    [Test]
    public async Task UserNames_FirstResultIsAlice()
    {
        EngineContext ctx = BuildContext();
        UserNamesResult first = await default(UserNames).ExecuteAsync(ctx).FirstAsync();
        await Assert.That(first.name).IsEqualTo("Alice");
    }

    [Test]
    public async Task AdminLogins_ReturnsAdminLogins()
    {
        EngineContext ctx = BuildContext();
        System.Collections.Generic.List<AdminLoginsResult> results =
            await default(AdminLogins).ExecuteAsync(ctx).ToListAsync();

        await Assert.That(results.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Execute_EmptyFactTable_ReturnsNoResults()
    {
        var ctx = new EngineContext();
        ctx.Users = new FactTable<User>(System.Array.Empty<User>());

        System.Collections.Generic.List<UserNamesResult> results =
            await default(UserNames).ExecuteAsync(ctx).ToListAsync();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    [Test]
    public async Task MatchingUserNames_ReturnsNamesFromNestedLookup()
    {
        EngineContext ctx = BuildContext();
        List<MatchingUserNamesResult> results = default(MatchingUserNames).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(3);
        await Assert.That(results[0].login).IsEqualTo("alice");
        await Assert.That(results[0].name).IsEqualTo("Alice");
        await Assert.That(results[1].login).IsEqualTo("bob");
        await Assert.That(results[1].name).IsEqualTo("Bob");
        await Assert.That(results[2].login).IsEqualTo("carol");
        await Assert.That(results[2].name).IsEqualTo("Carol");
    }
}

// ── Sync (IEnumerable<T>) API coverage ───────────────────────────────────────

public class SimpleTests_Execute
{
    private static EngineContext BuildContext()
    {
        var ctx = new EngineContext();
        ctx.Users = new FactTable<User>(new[]
        {
            new User("alice", "Alice", true),
            new User("bob",   "Bob",   false),
            new User("carol", "Carol", true),
        });
        return ctx;
    }

    [Test]
    public async Task UserNames_ReturnsAllUsers()
    {
        EngineContext ctx = BuildContext();
        List<UserNamesResult> results = default(UserNames).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(3);
    }

    [Test]
    public async Task UserNames_FirstResultIsAlice()
    {
        EngineContext ctx = BuildContext();
        UserNamesResult first = default(UserNames).Execute(ctx).First();

        await Assert.That(first.name).IsEqualTo("Alice");
    }

    [Test]
    public async Task AdminLogins_ReturnsAdminLogins()
    {
        EngineContext ctx = BuildContext();
        List<AdminLoginsResult> results = default(AdminLogins).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Execute_EmptyFactTable_ReturnsNoResults()
    {
        var ctx = new EngineContext();
        ctx.Users = new FactTable<User>(System.Array.Empty<User>());

        List<UserNamesResult> results = default(UserNames).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Execute_AndExecuteAsync_ReturnEquivalentResults()
    {
        EngineContext ctx = BuildContext();

        List<UserNamesResult> sync = default(UserNames).Execute(ctx).ToList();
        List<UserNamesResult> async_ = await default(UserNames).ExecuteAsync(ctx).ToListAsync();

        await Assert.That(sync.Count).IsEqualTo(async_.Count);
        for (int i = 0; i < sync.Count; i++)
            await Assert.That(sync[i]).IsEqualTo(async_[i]);
    }
}
