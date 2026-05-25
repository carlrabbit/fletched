# Getting Started

```csharp
using Fletched.Core;
using Fletched.Core.Runtime;

[Fact]
public readonly partial record struct Person(string Name, int Age);

[Predicate]
public readonly partial record struct AdultNames
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<Person>(p =>
            p.Name == name &&
            p.Age >= 18);
}

var ctx = new EngineContext();
ctx.Persons = new FactTable<Person>(new[] { new Person("Alice", 42) });

foreach (var result in default(AdultNames).Execute(ctx))
{
    Console.WriteLine(result.name);
}
```
