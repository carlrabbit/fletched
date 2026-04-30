# GitHub Copilot Instructions

## Coding Standards

- Target **C# 14** and **.NET 10** features where appropriate.
- Use `PascalCase` for types and public members; `camelCase` for local variables and parameters.
- Use **primary constructors** (C# 12+) for constructor injection where appropriate.
- Prefer **file-scoped namespaces** (e.g., `namespace Fletched.Features;`).
- Use **nullable reference types** – all projects have `<Nullable>enable</Nullable>`.
- Use **async/await** for all I/O-bound operations; suffix async methods with `Async`.
- Use **`ILogger<T>`** for logging; never use `Console.Write` in production code.
- Prefer **`IConfiguration`** for configuration over hard-coded values.
- Use **pattern matching** and **switch expressions** over chains of `if/else` where it improves clarity.
- Avoid `var` when the type is not immediately obvious from the right-hand side.

## Dependency Injection

- Register services in `Program.cs` using the built-in DI container (`Microsoft.Extensions.Hosting`).
- Prefer constructor injection over service locator pattern.
- Scope services appropriately: singleton for stateless services, scoped for per-operation services, transient for lightweight services.

## Testing

- Use **TUnit** for all tests (backed by Microsoft Testing Platform).
- Use **Moq** for mocking dependencies.
- Follow the **Arrange / Act / Assert** pattern.
- All TUnit assertions return `Task` and **must be awaited** (`await Assert.That(...).IsEqualTo(...)`).
- Name tests using the convention: `MethodName_StateUnderTest_ExpectedBehavior`.
- Place unit tests in `tests/LaughingBarnacle.Tests/`.

## Security

- Never include secrets, connection strings, or API keys in source code.
- Use `dotnet user-secrets` for local development secrets.
- Use environment variables or Azure Key Vault in production.
- Validate and sanitize all inputs; use parameterized queries for any database access.

## Pull Request Guidelines

- Every PR must include tests for new or changed behaviour.
- Copilot suggestions must never include hardcoded credentials or sensitive data.
- Keep PRs focused; one feature or fix per PR.

## Agent Behaviour

- Ask clarifying questions before generating large scaffolding or architectural changes.
- When generating new files, follow the existing project structure and naming conventions above.
- Prefer minimal, targeted changes over large rewrites.
