# GitHub Codespaces and VS Code setup

This repository now includes a shared Codespaces configuration in
`.devcontainer/devcontainer.json`.

## Best practices for this repository

- Use the repository dev container as the single shared development baseline so
  Codespaces and local VS Code dev containers stay aligned.
- Keep the container image close to CI. This repository uses the same .NET 10
  SDK family as the GitHub Actions workflows.
- Put deterministic setup in `updateContentCommand` so GitHub Codespaces
  prebuilds can cache it. For this project that means restoring and building the
  solution, not starting long-running background processes.
- Keep editor setup lightweight and shared. Only essential VS Code extensions
  and settings should be checked in.
- Avoid putting secrets in the repository. Use Codespaces secrets or repository
  secrets for anything like private NuGet feeds or API keys.
- Let CI remain the source of truth for full validation. The prebuild prepares a
  ready-to-code environment; it does not replace the existing workflows.

## What the checked-in configuration does

The dev container configuration is designed to give contributors a working .NET
10 environment with minimal startup friction:

- starts from a .NET 10 SDK Linux container image
- disables noisy first-run telemetry/output for the CLI
- installs a small set of C# and EditorConfig VS Code extensions
- sets `Fletched.slnx` as the default solution in VS Code
- enables format-on-save
- runs `dotnet restore Fletched.slnx`
- runs `dotnet build Fletched.slnx -c Release --no-restore`

Using `updateContentCommand` means those restore/build steps can be executed as
part of a Codespaces prebuild and then reused by newly created codespaces.

## Recommended workflow inside the codespace

After the codespace opens, the common repository commands stay the same:

```bash
dotnet build Fletched.slnx -c Release
dotnet run --no-build -c Release --project tests/Fletched.Core.Tests/Fletched.Core.Tests.csproj
dotnet run --no-build -c Release --project tests/Fletched.Features.Tests/Fletched.Features.Tests.csproj
dotnet run --no-build -c Release --project tests/Fletched.Integration.Tests/Fletched.Integration.Tests.csproj
dotnet run --no-build -c Release --project tests/WorkAssignment.Tests/WorkAssignment.Tests.csproj
```

## Manual GitHub setup still required after this PR

Some Codespaces settings live in GitHub repository or organization settings and
cannot be enabled from a pull request:

1. Enable GitHub Codespaces for the repository or organization if it is not
   already available.
2. Turn on Codespaces prebuilds in **Settings → Codespaces → Prebuild
   configurations** and point them at `.devcontainer/devcontainer.json`.
3. Choose which branches should receive prebuilds, typically the default branch
   and any long-lived development branches.
4. Add Codespaces secrets if private package feeds or external services are ever
   needed.
5. Optionally choose a larger default machine type if contributors find the
   solution build slow on the smallest codespace size.

The checked-in configuration is ready for those GitHub-side settings, but the
settings themselves still need to be enabled by a repository maintainer.
