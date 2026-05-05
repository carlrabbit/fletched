using System.Collections.Generic;
using System.Linq;
using Fletched.Core;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Integration.Tests;

// ── Domain model ─────────────────────────────────────────────────────────────

/// <summary>An employee with a name and an admin flag.</summary>
[Fact]
public partial record struct Employee(string Name, bool IsAdmin);

/// <summary>A product with a name and a price.</summary>
[Fact]
public partial record struct Product(string Name, int Price);

// ── Predicates ────────────────────────────────────────────────────────────────

/// <summary>Returns the names of non-admin employees (those where IsAdmin is not true).</summary>
[Predicate]
public partial record struct NonAdminEmployees
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<Employee>(e => e.Name == name && Logic.Not(e.IsAdmin == true));
}

/// <summary>Returns products with prices that are not above the given threshold.</summary>
[Predicate]
public partial record struct AffordableProducts
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<Product>(p => p.Name == name && Logic.Not(p.Price > 100));
}

/// <summary>Returns products that are not in the low-price range (not both Name != "Cheap" and price &lt;= 50).</summary>
[Predicate]
public partial record struct PremiumProducts
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<Product>(p => p.Name == name && Logic.Not(p.Price <= 50));
}

// ── Tests ─────────────────────────────────────────────────────────────────────

public class NotTests
{
    private static EngineContext BuildEmployeeContext()
    {
        var ctx = new EngineContext();
        ctx.Employees = new FactTable<Employee>(new[]
        {
            new Employee("Alice", true),
            new Employee("Bob",   false),
            new Employee("Carol", true),
            new Employee("Dave",  false),
        });
        return ctx;
    }

    private static EngineContext BuildProductContext()
    {
        var ctx = new EngineContext();
        ctx.Products = new FactTable<Product>(new[]
        {
            new Product("Budget",   30),
            new Product("Standard", 80),
            new Product("Premium",  150),
            new Product("Luxury",   300),
        });
        return ctx;
    }

    // ── NonAdminEmployees (Not with unification) ──────────────────────────────

    [Test]
    public async Task NonAdminEmployees_ReturnsOnlyNonAdmins()
    {
        EngineContext ctx = BuildEmployeeContext();
        List<NonAdminEmployeesResult> results =
            default(NonAdminEmployees).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(2);
    }

    [Test]
    public async Task NonAdminEmployees_ContainsBob()
    {
        EngineContext ctx = BuildEmployeeContext();
        bool hasBob = default(NonAdminEmployees).Execute(ctx)
            .Any(r => r.name == "Bob");

        await Assert.That(hasBob).IsTrue();
    }

    [Test]
    public async Task NonAdminEmployees_ContainsDave()
    {
        EngineContext ctx = BuildEmployeeContext();
        bool hasDave = default(NonAdminEmployees).Execute(ctx)
            .Any(r => r.name == "Dave");

        await Assert.That(hasDave).IsTrue();
    }

    [Test]
    public async Task NonAdminEmployees_ExcludesAlice()
    {
        EngineContext ctx = BuildEmployeeContext();
        bool hasAlice = default(NonAdminEmployees).Execute(ctx)
            .Any(r => r.name == "Alice");

        await Assert.That(hasAlice).IsFalse();
    }

    [Test]
    public async Task NonAdminEmployees_ExcludesCarol()
    {
        EngineContext ctx = BuildEmployeeContext();
        bool hasCarol = default(NonAdminEmployees).Execute(ctx)
            .Any(r => r.name == "Carol");

        await Assert.That(hasCarol).IsFalse();
    }

    // ── AffordableProducts (Not with comparison) ──────────────────────────────

    [Test]
    public async Task AffordableProducts_ExcludesExpensiveProducts()
    {
        EngineContext ctx = BuildProductContext();
        List<AffordableProductsResult> results =
            default(AffordableProducts).Execute(ctx).ToList();

        // Budget (30) and Standard (80) are ≤ 100; Premium (150) and Luxury (300) are > 100
        await Assert.That(results.Count).IsEqualTo(2);
    }

    [Test]
    public async Task AffordableProducts_ContainsBudget()
    {
        EngineContext ctx = BuildProductContext();
        bool hasBudget = default(AffordableProducts).Execute(ctx)
            .Any(r => r.name == "Budget");

        await Assert.That(hasBudget).IsTrue();
    }

    [Test]
    public async Task AffordableProducts_ContainsStandard()
    {
        EngineContext ctx = BuildProductContext();
        bool hasStandard = default(AffordableProducts).Execute(ctx)
            .Any(r => r.name == "Standard");

        await Assert.That(hasStandard).IsTrue();
    }

    [Test]
    public async Task AffordableProducts_ExcludesPremium()
    {
        EngineContext ctx = BuildProductContext();
        bool hasPremium = default(AffordableProducts).Execute(ctx)
            .Any(r => r.name == "Premium");

        await Assert.That(hasPremium).IsFalse();
    }

    [Test]
    public async Task AffordableProducts_ExcludesLuxury()
    {
        EngineContext ctx = BuildProductContext();
        bool hasLuxury = default(AffordableProducts).Execute(ctx)
            .Any(r => r.name == "Luxury");

        await Assert.That(hasLuxury).IsFalse();
    }

    // ── PremiumProducts (Not with comparison <=) ──────────────────────────────

    [Test]
    public async Task PremiumProducts_ExcludesCheapProducts()
    {
        EngineContext ctx = BuildProductContext();
        List<PremiumProductsResult> results =
            default(PremiumProducts).Execute(ctx).ToList();

        // Budget (30) and Standard (80) have price ≤ 50 only for Budget; Standard > 50
        // Not(price <= 50) means price > 50: Standard (80), Premium (150), Luxury (300)
        await Assert.That(results.Count).IsEqualTo(3);
    }

    [Test]
    public async Task PremiumProducts_ExcludesBudget()
    {
        EngineContext ctx = BuildProductContext();
        bool hasBudget = default(PremiumProducts).Execute(ctx)
            .Any(r => r.name == "Budget");

        await Assert.That(hasBudget).IsFalse();
    }

    [Test]
    public async Task PremiumProducts_ContainsPremium()
    {
        EngineContext ctx = BuildProductContext();
        bool hasPremium = default(PremiumProducts).Execute(ctx)
            .Any(r => r.name == "Premium");

        await Assert.That(hasPremium).IsTrue();
    }

    // ── Empty table edge case ─────────────────────────────────────────────────

    [Test]
    public async Task NonAdminEmployees_EmptyTable_ReturnsNoResults()
    {
        var ctx = new EngineContext();
        ctx.Employees = new FactTable<Employee>(System.Array.Empty<Employee>());

        List<NonAdminEmployeesResult> results =
            default(NonAdminEmployees).Execute(ctx).ToList();

        await Assert.That(results.Count).IsEqualTo(0);
    }
}
