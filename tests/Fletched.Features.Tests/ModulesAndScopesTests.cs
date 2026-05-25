using Fletched.Core;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Features.Tests;

[Module]
public static partial class IdentityModule
{
    [Fact]
    [FactIndex(nameof(User.Name))]
    public partial record struct User(string Login, string Name);

    [Predicate]
    public partial record struct UserNames
    {
        [PredicateBody]
        public static LogicExpr<bool> Body(TerminalVar<string> name) =>
            Logic.With<User>(user => user.Name == name);
    }
}

public static partial class ScopedQueries
{
    [Predicate]
    public partial record struct PersonLogins
    {
        [PredicateBody]
        public static LogicExpr<bool> Body(TerminalVar<string> login) =>
            Logic.With<Person>(person => person.Login == login);
    }
}

public class ModulesAndScopesTests
{
    [Test]
    public async Task ModuleFact_GeneratesModuleScopedEngineContext()
    {
        var ctx = new IdentityModule.EngineContext();
        ctx.Users = new FactTable<IdentityModule.User>(new[]
        {
            new IdentityModule.User("alice", "Alice"),
            new IdentityModule.User("bob", "Bob"),
        });

        await Assert.That(ctx.Users.Data.Length).IsEqualTo(2);
    }

    [Test]
    public async Task ModulePredicate_QueryWrapper_ReturnsScopedResults()
    {
        var ctx = new IdentityModule.EngineContext();
        ctx.Users = new FactTable<IdentityModule.User>(new[]
        {
            new IdentityModule.User("alice", "Alice"),
            new IdentityModule.User("bob", "Bob"),
        });

        var results = IdentityModule.Query_UserNames(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(2);
        await Assert.That(results[0].name).IsEqualTo("Alice");
        await Assert.That(results[1].name).IsEqualTo("Bob");
    }

    [Test]
    public async Task ScopeOnlyPredicate_Execute_ReturnsScopedResults()
    {
        var ctx = new EngineContext();
        ctx.Persons = new FactTable<Person>(new[]
        {
            new Person("alice", "Alice"),
            new Person("bob", "Bob"),
        });

        var results = default(ScopedQueries.PersonLogins).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(2);
        await Assert.That(results[0].login).IsEqualTo("alice");
        await Assert.That(results[1].login).IsEqualTo("bob");
    }
}
