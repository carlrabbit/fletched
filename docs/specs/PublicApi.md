# Public API Contract

## Scope

This specification defines the intentional public package contract for:
- `Fletched.Core`
- `Fletched.Roslyn`

## Public Namespaces

`Fletched.Core` intentionally exposes consumer APIs under:
- `Fletched.Core`
- `Fletched.Core.Runtime`
- `Fletched.Core.Diagnostics` (if present)

`Fletched.Roslyn` is delivered as analyzer/source-generator package content and is not intended as runtime API.

## Public Attributes

- `FactAttribute`
- `PredicateAttribute`
- `PredicateBodyAttribute`
- `ModuleAttribute`
- `FactIndexAttribute`

## Public DSL Entry Points

- `LogicExpr<T>`
- `Logic`
- `Logic.With<...>`
- `LogicList<T>`
- `Proxy<T>`
- `TerminalVar<T>`

## Public Runtime APIs

- `EngineContext`
- `FactTable<T>`
- `QueryExecutionOptions`
- `QueryMetricsSnapshot`
- intentionally public query exception types

## Public Diagnostics Surface

Public diagnostic IDs are documented in `public-docs/diagnostics.md` and originate from `Fletched.Roslyn` analyzer/source generator.

## Query/Result API Shape

Generated query/result APIs are public package-facing behavior and are versioned via public API baselines under `public-docs/api-baselines/`.

## Supported Package References

```xml
<ItemGroup>
  <PackageReference Include="Fletched.Core" Version="0.2.0" />
  <PackageReference Include="Fletched.Roslyn" Version="0.2.0" PrivateAssets="all" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

## Unsupported/Internal Namespaces

The following are internal-only and unsupported as public contracts:
- `Fletched.Internal`
- `Fletched.Roslyn.*`
- `Fletched.Compiler.*`
- `Fletched.IR.*`
- `Fletched.Planning.*`

## Compatibility Policy

- `0.1.0.0` was a premature release and has no compatibility guarantees.
- `0.2.x` is the first intentional pre-1.0 line.
- In `0.x`, source and binary compatibility can break between minor versions when needed.
- `1.0.0` is the first stable compatibility target for source/binary API contracts.
