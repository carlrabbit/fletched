using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fletched.Core;
using Fletched.Core.Runtime;
using TUnit;

namespace Fletched.Integration.Tests;

[Fact]
public partial record struct ArithmeticNumber(int Value);

[Predicate]
public partial record struct ArithmeticExpanded
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<int> value) =>
        Logic.With<ArithmeticNumber>(n =>
            n.Value == value &&
            n.Value * 3 > 5 &&
            n.Value / 2 >= 1 &&
            n.Value % 2 == 0 &&
            -n.Value < 0);
}

public class ArithmeticExpansionTests
{
    [Test]
    public async Task ArithmeticExpanded_UsesExpandedOperators(CancellationToken ct)
    {
        var ctx = new EngineContext
        {
            ArithmeticNumbers = new FactTable<ArithmeticNumber>(new[]
            {
                new ArithmeticNumber(1),
                new ArithmeticNumber(2),
                new ArithmeticNumber(3),
                new ArithmeticNumber(4),
            })
        };

        List<ArithmeticExpandedResult> results = await default(ArithmeticExpanded).ExecuteAsync(ctx).ToListAsync(ct);

        string values = string.Join(",", results.Select(result => result.value));
        await Assert.That(values).IsEqualTo("2,4");
    }
}
