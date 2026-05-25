# Fletched.Core

Typed runtime and DSL primitives for Fletched, a source-generated logic programming engine for .NET.

## Install

```xml
<PackageReference Include="Fletched.Core" Version="0.2.0" />
```

Use this package together with `Fletched.Roslyn` for source generation and diagnostics.

## Start Here

- `FactAttribute`
- `PredicateAttribute`
- `PredicateBodyAttribute`
- `Logic`
- `TerminalVar<T>`
- `EngineContext`
- `FactTable<T>`

## Minimal Example

```csharp
using Fletched.Core;
using Fletched.Core.Runtime;

[Fact]
public readonly partial record struct Person(string Name, int Age);
```

## Versioning Note

- `0.2.0` is the first intentional pre-1.0 package line.
- `0.1.0.0` was premature and had no compatibility guarantees.
