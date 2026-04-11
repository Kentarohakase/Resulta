# Resulta (core package)

This folder contains the **Resulta** class library (`Resulta.csproj`) — the main NuGet package source.

For full documentation, installation, examples, and repository layout, see the **[root README](../README.md)** in the repository.

## Package layout

- **Source:** `src/` (`Result.cs`, `ResultT.cs`, `Error.cs`, `ResultExtensions.cs`)
- **Extensions:** `extensions/` (`ValidationResult.cs`, `Pipeline.cs`)

Optional packages live in sibling folders: `Resulta.AspNetCore/`, `Resulta.FluentValidation/`.

Runnable demo code is in **`samples/Resulta.Samples/`** (console app, not published as a package).
