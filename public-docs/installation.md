# Installation

Install runtime and generator packages:

```xml
<ItemGroup>
  <PackageReference Include="Fletched.Core" Version="0.2.0" />
  <PackageReference Include="Fletched.Roslyn" Version="0.2.0" PrivateAssets="all" OutputItemType="Analyzer" ReferenceOutputAssembly="false" />
</ItemGroup>
```

`Fletched.Roslyn` is required at compile-time for source generation and diagnostics.
