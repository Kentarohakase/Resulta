# Versioning Guide

**Resulta** follows **Semantic Versioning**:

`MAJOR.MINOR.PATCH`

Examples:
- `1.0.0` → first stable release
- `1.1.0` → new backwards-compatible feature
- `1.0.1` → backwards-compatible bug fix
- `2.0.0` → breaking change

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

## NuGet publishing policy

NuGet packages are published automatically via GitHub Actions on every **MINOR** or **MAJOR** version tag.

- `v2.1.0` → published to NuGet ✅
- `v2.0.1` → pushed to GitHub only, no NuGet publish ❌

Patch releases are intentionally not published to NuGet to keep the feed clean.

---

## Release checklist

Before every release:

1. Update the version in all three `.csproj` files (`Resulta`, `Resulta.AspNetCore`, `Resulta.FluentValidation`).
2. Add the new entry to `CHANGELOG.md`.
3. Commit and push changes to `main`.
4. Push a version tag — GitHub Actions handles the rest automatically.

---

## Release commands

### 1. Update the version in all `.csproj` files

```xml
<Version>2.2.0</Version>
```

### 2. Commit and push

```bash
git add .
git commit -m "chore: bump version to 2.2.0"
git push origin main
```

### 3. Tag the release — this triggers the full CI/CD pipeline

```bash
git tag v2.2.0
git push origin v2.2.0
```

GitHub Actions will then automatically:
- Build and run all tests
- Pack the NuGet packages
- Publish to NuGet (MINOR/MAJOR only)
- Create a GitHub Release

---

## Practical rule of thumb

Ask this before every release:

- Did I break existing user code? → **MAJOR**
- Did I add new functionality without breaking anything? → **MINOR**
- Did I only fix something? → **PATCH**
