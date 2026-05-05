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
        await Assert.That(true).IsTrue();
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
}
