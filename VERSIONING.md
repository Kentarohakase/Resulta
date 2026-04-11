# Versioning Guide

**Resulta** follows **Semantic Versioning**:

`MAJOR.MINOR.PATCH`

Example:
- `1.0.0` -> first stable release
- `1.1.0` -> new backwards compatible feature
- `1.0.1` -> backwards compatible bug fix
- `2.0.0` -> breaking change

## When to bump which version?

### MAJOR
Increase the **major** version when you introduce **breaking changes**.

This includes changes that can make existing consumer code stop compiling or behave differently.

Examples:
- Renaming or removing a public method
- Changing a public method signature
- Changing namespaces or public types
- Changing behavior in a way that breaks existing integrations

Example:
- `1.0.0` -> `2.0.0`

### MINOR
Increase the **minor** version when you add **new functionality** in a fully backwards compatible way.

Examples:
- Adding new helper methods such as `OkIf` or `FailIf`
- Adding new extension methods
- Adding a new integration package or bridge
- Adding new pipeline or validation helpers without breaking existing APIs

Example:
- `1.0.0` -> `1.1.0`

### PATCH
Increase the **patch** version when you release **bug fixes** or small internal improvements without changing the public API in a breaking way.

Examples:
- Fixing a null reference exception
- Fixing a wrong error code mapping
- Fixing edge case behavior
- Fixing XML documentation or packaging metadata

Example:
- `1.0.0` -> `1.0.1`

## Release checklist

Before every release:

1. Update the version in the project file.
2. Add the new entry to `CHANGELOG.md`.
3. Commit and push changes to GitHub.
4. Create a GitHub Release with tag `vX.Y.Z`.
5. Build and pack the NuGet package.
6. Push the package to NuGet.

## Package versions in this repository

**Resulta**, **Resulta.AspNetCore**, and **Resulta.FluentValidation** use the **same `<Version>`** in their `.csproj` files and are released in lockstep (one semantic version for the whole repo). Consumers should reference matching versions across packages to avoid API or behavior skew.

Update the version in all three project files for each release:

- `Resulta/Resulta.csproj`
- `Resulta.AspNetCore/Resulta.AspNetCore.csproj`
- `Resulta.FluentValidation/Resulta.FluentValidation.csproj`

## Release commands

### 1. Update the version in each project file

```xml
<Version>2.1.1</Version>
```

Use the same value everywhere (patch / minor / major per the rules above).

### 2. Build and pack

```bash
dotnet build -c Release
dotnet pack -c Release
```

### 3. Push to NuGet

From the repository root (adjust paths if your output layout differs):

```bash
dotnet nuget push .\Resulta\bin\Release\Resulta.X.Y.Z.nupkg ^
  --api-key YOUR_API_KEY ^
  --source https://api.nuget.org/v3/index.json
```

Repeat for `Resulta.AspNetCore` and `Resulta.FluentValidation` packages as needed.

### 4. Tag the release on GitHub

```bash
git tag v2.1.1
git push origin v2.1.1
```

## Practical rule of thumb

Ask this before every release:

- Did I break existing user code? -> **MAJOR**
- Did I add new functionality without breaking anything? -> **MINOR**
- Did I only fix something? -> **PATCH**
