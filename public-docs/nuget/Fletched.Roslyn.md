# Fletched.Roslyn

Roslyn source generator and analyzers for Fletched typed logic predicates.

## Install

```xml
<PackageReference Include="Fletched.Roslyn" Version="0.2.0" PrivateAssets="all" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
```

Use with `Fletched.Core` runtime package.

## Minimal Generator Example

```csharp
using Fletched.Core;

[Predicate]
public readonly partial record struct Adult
{
    [PredicateBody]
    public static LogicExpr<bool> Body(TerminalVar<string> name) =>
        Logic.With<Person>(p => p.Name == name && p.Age >= 18);
}
```

## Diagnostics Overview

The package surfaces compile-time diagnostics for invalid DSL, unsupported patterns, and index misuse.

## Troubleshooting

- Ensure analyzer reference metadata exactly matches the install snippet.
- Clear `obj/` and `bin/` then rebuild if generated code appears stale.

## Versioning Note

- `0.2.0` is the first intentional pre-1.0 package line.
- `0.1.0.0` was premature and had no compatibility guarantees.
