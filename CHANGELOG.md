# Changelog

All notable changes to **Resulta** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- Nothing yet.

### Changed
- Reworked `Result`, `Result<T>`, and `ValidationResult<T>` to use class-based implementations instead of structs.
- Updated example code to match the current API shape and compile cleanly.
- Renamed project structure, solution, and project files from `FluentResults` to `Resulta`.
- Aligned ASP.NET Core extension methods and namespaces with the `Resulta` package name.
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

[Unreleased]: https://github.com/Kentarohakase/Resulta/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/Kentarohakase/Resulta/releases/tag/v1.0.0
