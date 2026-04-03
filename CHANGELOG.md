# Changelog

All notable changes to **Resulta** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added
- Nothing yet.

### Changed
- Nothing yet.

### Fixed
- Nothing yet.

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
