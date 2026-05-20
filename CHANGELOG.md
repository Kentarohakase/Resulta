# Changelog

All notable changes to **Resulta** will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]


## [3.0.0] - 2026-05-20

This release modernizes the ASP.NET Core integration around RFC 7807 `ProblemDetails`,
adds first-class `TypedResults` support for Minimal API, ships System.Text.Json converters
for `Result`/`Error`, and adds OpenAPI helpers for the standard Resulta error responses.

### Added
- **Core**: `Resulta.Json.ResultaJsonConverterFactory` and the underlying `ResultJsonConverter`,
  `ResultJsonConverter<T>`, and `ErrorJsonConverter`. Opt in with
  `JsonSerializerOptions.AddResultaConverters()`. JSON is discriminated by `isSuccess`;
  errors expose `message`, `code`, optional `field`, optional `exceptionMessage` (never the
  stack trace), and a recursive `causedBy` chain truncated at three levels.
- **AspNetCore**: `ResultProblemDetailsFactory.Create(Error, HttpContext?)` builds an
  RFC 7807 `ProblemDetails` (or `HttpValidationProblemDetails` for validation errors) from
  any Resulta `Error`. The original `Error.Code` is preserved as the `code` extension property.
- **AspNetCore**: `ProblemTypeUris` exposes the canonical RFC `type` URIs Resulta uses
  (`NotFound`, `BadRequest`, `Unauthorized`, `Conflict`, `InternalServerError`).
- **AspNetCore**: `TypedMinimalApiExtensions.ToTypedResult` returns
  `Results<Ok<T>, NotFound<ProblemDetails>, BadRequest<HttpValidationProblemDetails>, Conflict<ProblemDetails>, ProblemHttpResult>`
  so OpenAPI / Swagger / generated client SDKs see every possible response shape.
- **AspNetCore**: `MinimalApiExtensions.ToMinimalApiResult` non-generic overload for `Result`
  (returns 204 on success).
- **AspNetCore**: `RouteHandlerBuilder.ProducesResultaErrors()` registers the standard
  Resulta error responses (400/401/404/409/500) on a Minimal API endpoint in one call.
  A `params int[]` overload allows opting in to a subset.

### Changed (BREAKING)
- **AspNetCore**: `ToActionResult`, `ToActionResult<T>`, `ToMinimalApiResult<T>` and
  `ResultMiddleware` now respond with `application/problem+json` and a `ProblemDetails`
  (or `HttpValidationProblemDetails`) body on failure. HTTP status codes are unchanged.
  Clients that depended on the exact `ErrorResponse` JSON layout from 2.x must adopt
  the standard RFC 7807 shape (`status`, `title`, `type`, `detail`, `instance`,
  `code` extension).
- **AspNetCore**: Validation errors now place the field name and message in
  `HttpValidationProblemDetails.Errors` (e.g. `{"errors": {"email": ["Invalid"]}}`)
  rather than a custom top-level `field` property.
- **AspNetCore**: Unknown error codes (anything outside `NOT_FOUND`, `VALIDATION_ERROR`,
  `UNAUTHORIZED`, `CONFLICT`) are flattened to a generic
  `500 Internal Server Error` with `code: "INTERNAL_ERROR"` and detail
  `"An internal error occurred."` — preserving the previous safety behavior of not
  leaking internal codes or exception messages to clients.

### Removed (BREAKING)
- **AspNetCore**: Public record `ErrorResponse` removed. Callers that explicitly
  referenced this type must migrate to `ProblemDetails` /
  `HttpValidationProblemDetails`, or map `Error` themselves.

### Migration notes for consumers
- **JSON parsing**: Replace any client code that read `{ "message", "code", "field" }`
  with code that reads `{ "title", "detail", "status", "type", "code" }` (and
  `errors` for validation). The `code` extension on `ProblemDetails` continues to expose
  the original `Error.Code`.
- **MVC return types**: Endpoint return types (`IActionResult`) and status codes are
  unchanged; only the response body shape differs. Most controllers need no changes.
- **Minimal API**: Switch to `ToTypedResult()` when you want OpenAPI to see every
  possible response shape; otherwise the existing `ToMinimalApiResult()` keeps working.
- **OpenAPI**: Chain `.ProducesResultaErrors()` onto Minimal API endpoint declarations
  to register the standard Resulta error responses.


## [2.1.7] - 2026-04-16

### Maintenance
- Series of release-pipeline fixes between 2.1.2 and 2.1.7 — adopted GitHub Actions
  Node 24, switched the release workflow to Windows runners, settled on the
  `dotnet nuget push` CLI with an API key after a brief experiment with trusted
  publishing. No library code changes.


## [2.1.1] - 2026-04-11

### Added
- Regression tests for defensive `Error` metadata copying, null-guards on `ResultExtensions`, pipelines, `Result<T>.Ok(null)` semantics, and ASP.NET Core HTTP mapping consistency.

### Changed
- **`Error`:** Constructor now always copies the optional metadata dictionary so external mutations cannot alter an error’s metadata (immutability).
- **`ResultExtensions`:** All public methods now validate non-null `result`, `task`, delegates, `error`, and collection arguments with `ArgumentNullException` (and `ArgumentException` for invalid `Ensure` messages).
- **`Pipeline<T>` / `AsyncPipeline<T>`:** Added the same style of null/whitespace guards for entry points, steps, validation, taps, and `Finally`; `AsyncPipeline.Start` rejects a null task from the factory.
- **`TaskResultExtensions.Bind`:** Validates `task` and `binder`.
- **`Resulta.AspNetCore`:** MVC (generic and non-generic) and Minimal API mappings are aligned — `VALIDATION_ERROR` includes optional `field` from metadata everywhere; non-generic MVC now maps `CONFLICT` and matches generic behavior; Minimal API returns a JSON `ErrorResponse` for `UNAUTHORIZED` (instead of an empty 401) and uses `500` + `INTERNAL_ERROR` for unknown codes (same as MVC).
- **Samples:** Demo code moved from `Resulta/Examples.cs` to `samples/Resulta.Samples/` so the core package build contains no sample entry points.
- **Docs:** `Result<T>.Ok` documents that `null` reference values are treated as success; root `README` and `VERSIONING.md` updated (removed stale FluentResults path references).

### Fixed
- Non-generic `ToActionResult` omitted `CONFLICT` and validation `field` parity with the generic overload.

### Notes for consumers
- **Minimal API:** `ToMinimalApiResult` previously returned an empty `401` for `UNAUTHORIZED` and `Results.Problem` for unknown codes; it now returns JSON `ErrorResponse` bodies aligned with MVC (see remarks on `ToMinimalApiResult`). Clients that assumed an empty 401 or Problem Details must be updated.
- **`Ensure` / pipeline `Validate`:** A null or whitespace-only message now throws `ArgumentException` immediately instead of creating an `Error` that would fail later in `Error`’s constructor.

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

[Unreleased]: https://github.com/Kentarohakase/Resulta/compare/v3.0.0...HEAD
[3.0.0]: https://github.com/Kentarohakase/Resulta/compare/v2.1.7...v3.0.0
[2.1.7]: https://github.com/Kentarohakase/Resulta/compare/v2.1.1...v2.1.7
[2.1.1]: https://github.com/Kentarohakase/Resulta/compare/v2.1.0...v2.1.1
[2.1.0]: https://github.com/Kentarohakase/Resulta/compare/v2.0.1...v2.1.0
[2.0.1]: https://github.com/Kentarohakase/Resulta/compare/v2.0.0...v2.0.1
[2.0.0]: https://github.com/Kentarohakase/Resulta/compare/v1.0.0...v2.0.0
[1.0.0]: https://github.com/Kentarohakase/Resulta/releases/tag/v1.0.0