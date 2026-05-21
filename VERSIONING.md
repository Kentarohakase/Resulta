# Versioning Guide

**Resulta** follows **Semantic Versioning**:

`MAJOR.MINOR.PATCH`

Examples:
- `1.0.0` -> first stable release
- `1.1.0` -> new backwards-compatible feature
- `1.0.1` -> backwards-compatible bug fix
- `2.0.0` -> breaking change

---

## When to bump which version?

### MAJOR
Increase the **major** version when you introduce **breaking changes**.

Examples:
- Renaming or removing a public method
- Changing a public method signature
- Changing namespaces or public types
- Changing behavior in a way that breaks existing integrations

### MINOR
Increase the **minor** version when you add **new functionality** in a fully backwards-compatible way.

Examples:
- Adding new helper methods such as `OkIf`, `FailIf`, or `CombineAsync`
- Adding new extension methods
- Adding a new integration package or bridge
- Adding new pipeline or validation helpers without breaking existing APIs

### PATCH
Increase the **patch** version when you release **bug fixes** or small internal improvements without changing the public API.

Examples:
- Fixing a null reference exception
- Fixing a wrong error code mapping
- Fixing edge case behavior
- Fixing XML documentation or packaging metadata

---

## Package versions in this repository

**Resulta**, **Resulta.AspNetCore**, and **Resulta.FluentValidation** use the same `<Version>` in their `.csproj` files and are released in lockstep. Consumers should reference matching versions across packages to avoid API or behavior skew.

Update the version in all three project files for each release:

- `Resulta/Resulta.csproj`
- `Resulta.AspNetCore/Resulta.AspNetCore.csproj`
- `Resulta.FluentValidation/Resulta.FluentValidation.csproj`

The packages target both `net8.0` and `net10.0`.

---

## NuGet publishing policy

GitHub Actions builds, tests, packs, and creates a GitHub Release for every `vX.Y.Z` tag.

NuGet packages are published only for **MAJOR** or **MINOR** release tags where the patch component is `0`.

- `v3.1.0` -> GitHub Release and NuGet publish
- `v4.0.0` -> GitHub Release and NuGet publish
- `v3.0.1` -> GitHub Release only, no NuGet publish

Patch releases are intentionally kept GitHub-only to keep the NuGet feed focused on stable feature and breaking-change releases.

---

## Release checklist

Before every release:

1. Update the version in all three package project files.
2. Add the new entry to `CHANGELOG.md`.
3. Run `dotnet restore Resulta.slnx`.
4. Run `dotnet build Resulta.slnx -c Release --no-restore`.
5. Run `dotnet test Resulta.slnx -c Release --framework net8.0`.
6. Run `dotnet test Resulta.slnx -c Release --framework net10.0`.
7. Commit and push changes to `master`.
8. Push a version tag.

---

## Release commands

### 1. Update the version in all package project files

```xml
<Version>3.1.0</Version>
```

### 2. Commit and push to master

```bash
git add .
git commit -m "chore: bump version to 3.1.0"
git push origin master
```

### 3. Tag the release

```bash
git tag v3.1.0
git push origin v3.1.0
```

GitHub Actions will then:
- Reject unresolved merge conflict markers
- Build and run all tests for `net8.0` and `net10.0`
- Pack all NuGet packages with both target framework assets
- Publish to NuGet only when the tag matches `vX.Y.0`
- Create a GitHub Release for every version tag

---

## Practical rule of thumb

Ask this before every release:

- Did I break existing user code? -> **MAJOR**
- Did I add new functionality without breaking anything? -> **MINOR**
- Did I only fix something? -> **PATCH**
