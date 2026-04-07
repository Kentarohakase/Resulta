# Changelog

All notable changes to **Resulta** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]


## [2.1.0] - 2026-04-07

### Added
- Added `Result.OkIf` and `Result.FailIf` factory methods, both in generic and non-generic variants, for concise conditional result creation.
- Added `ResultExtensions.CombineAsync` for awaiting and combining multiple async `Result<T>` tasks in parallel, available as both `params` and `IEnumerable` overloads.
- Added `Validate` (with string and `Error` overloads) to `AsyncPipeline<T>` for inline predicate validation within async pipelines.
- Added `Tap` to `AsyncPipeline<T>` for synchronous side effects within async pipelines.
- Added `TapAsync` to `AsyncPipeline<T>` for asynchronous side effects within async pipelines.
- Added full XML documentation to all public types across all packages, resolving all CS1591 compiler warnings.
- Added tests for `OkIf` / `FailIf`, `CombineAsync`, and the new `AsyncPipeline<T>` methods.


## [2.0.1] - 2026-04-07

### Fixed
- Fixed namespace ambiguity between `FluentValidation.Results.ValidationResult` and `Resulta.Extensions.ValidationResult<T>` in `FluentValidationBridge` by introducing a `using` alias (`FVResult`).
- Fixed `Pipeline<T>.ThenAsync` being restricted to the same type `T` — added a generic `ThenAsync<TOut>` overload to both `Pipeline<T>` and `AsyncPipeline<T>` to allow type changes between steps.
- Fixed missing `params` overload for `ResultExtensions.Combine<T>` — generic results can now be combined inline without wrapping in an array.
- Fixed broken Markdown code block in `Resulta/README.md` that caused all content after the Installation section to render incorrectly.
- Fixed missing space in `CHANGELOG.md` date for version `2.0.0` (`-2026-04-03` → `- 2026-04-03`).

### Removed
- Removed empty `UnitTest1` test class that produced a false-positive green result in CI.
- Removed orphaned stub projects under `src/` that were not referenced in the solution.


## [2.0.0] - 2026-04-03

### Added
- Added automated tests for `Result`, `Result<T>`, `ValidationResult<T>`, and `Validator<T>`.

### Changed
- Reworked `Result`, `Result<T>`, and `ValidationResult<T>` to use class-based implementations instead of structs.
- Updated example code to match the current API shape and compile cleanly.
- Renamed project structure, solution, and project files from `FluentResults` to `Resulta`.
- Aligned ASP.NET Core extension methods and namespaces with the `Resulta` package name.
- Moved ASP.NET Core integration into a dedicated `Resulta.AspNetCore` project.
- Moved FluentValidation integration into a dedicated `Resulta.FluentValidation` project.
- Removed ASP.NET Core and FluentValidation dependencies from the core `Resulta` package.
- Improved package separation between core functionality and optional integrations.

### Fixed
- Fixed invalid default-state behavior in result types.
- Fixed multiple example build issues caused by outdated API usage.


## [1.0.0] - 2026-04-03

### Added
- `Result` for non-generic result handling.
- `Result<T>` for typed success and failure flows.
- `Error` as a structured error model with code, metadata, cause chain, and exception support.
- Predefined error factories such as `NotFound`, `Validation`, `Unauthorized`, `Conflict`, and `Unexpected`.
- Core fluent operations including `Map`, `Bind`, `Match`, and `Ensure`.
- `ResultExtensions` with helpers like `Try`, `TryAsync`, `Combine`, `MapAsync`, and `BindAsync`.
- `ValidationResult<T>` for collecting multiple validation errors.
- `Validator<T>` as a fluent validation builder.
- `Pipeline<T>` for synchronous Railway Oriented Programming.
- `AsyncPipeline<T>` for asynchronous Railway Oriented Programming.
- ASP.NET Core integration including `ToActionResult`, `ToMinimalApiResult`, `AddResulta`, and `UseResulta`.
- FluentValidation bridge helpers including sync and async validation to `Result` conversion.
- Implicit conversions from values and errors to `Result<T>`.
- `.NET 10` support.

[Unreleased]: https://github.com/Kentarohakase/Resulta/compare/v2.1.0...HEAD
[2.1.0]: https://github.com/Kentarohakase/Resulta/compare/v2.0.1...v2.1.0
[2.0.1]: https://github.com/Kentarohakase/Resulta/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/Kentarohakase/Resulta/compare/v1.0.0...v2.0.0
[1.0.0]: https://github.com/Kentarohakase/Resulta/releases/tag/v1.0.0