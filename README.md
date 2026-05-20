# Resulta 🎯

A lightweight C# library for the **Result pattern** – error handling without exceptions.

[![NuGet](https://img.shields.io/nuget/v/Resulta)](https://www.nuget.org/packages/Resulta)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Resulta)](https://www.nuget.org/packages/Resulta)
[![CI](https://github.com/Kentarohakase/Resulta/actions/workflows/Ci.yml/badge.svg)](https://github.com/Kentarohakase/Resulta/actions/workflows/Ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com)

---

## Why Resulta?

Instead of:
```csharp
// ❌ Exceptions as control flow – hard to read, easy to forget
try {
    var user = GetUser(id);  // throws NotFoundException
    return Ok(user);
} catch (NotFoundException ex) {
    return NotFound(ex.Message);
} catch (Exception ex) {
    return StatusCode(500, ex.Message);
}
```

Use this:
```csharp
// ✅ Explicit, type-safe, no try/catch
return GetUser(id).Match<IActionResult>(
    onSuccess: user => Ok(user),
    onFailure: err  => err.Code switch
    {
        "NOT_FOUND" => NotFound(err.Message),
        _           => StatusCode(500, err.Message)
    }
);
```

---

## Installation

### Core package

```bash
dotnet add package Resulta
```

### Optional integrations

```bash
dotnet add package Resulta.AspNetCore
dotnet add package Resulta.FluentValidation
```

---

## Quick Start

### Ok & Fail

```csharp
using Resulta;

Result<int> Divide(int a, int b)
{
    if (b == 0)
        return Result.Fail<int>("Division by zero!");

    return Result.Ok(a / b);
}

var result = Divide(10, 2);

if (result.IsSuccess)
    Console.WriteLine(result.Value);    // 5
else
    Console.WriteLine(result.Error);    // error message
```

### OkIf & FailIf

```csharp
var result = Result.OkIf(user.IsActive, user, Error.Unauthorized("Account is inactive"));

var conflict = Result.FailIf(exists, resource, Error.Conflict("Already exists"));
```

### Map & Bind – Chaining

```csharp
var dto = LoadUser(1)
    .Bind(ConvertToDto)
    .Ensure(d => d.Email.Contains('@'), "Not a valid email")
    .Map(d => d with { Name = d.Name.Trim() });
```

### Match – handle both cases

```csharp
string response = result.Match(
    onSuccess: value => $"Result: {value}",
    onFailure: err   => $"Error: {err.Message}"
);
```

### Success and null (reference types)

For reference types, `Result<T>.Ok(value)` still represents **success** when `value` is `null`. If `null` is invalid in your domain, validate explicitly and return `Result<T>.Fail(...)` instead. Value types are unaffected (`Result<int>.Ok` always carries a value).

### Try – catch exceptions

```csharp
var result = ResultExtensions.Try(
    () => int.Parse(input),
    ex  => new Error("Invalid number").WithCode("PARSE_ERROR")
);
```

### CombineAsync – parallel async operations

```csharp
var result = await ResultExtensions.CombineAsync(
    LoadUserAsync(id),
    LoadOrderAsync(id),
    LoadAddressAsync(id)
);
```

---

## Error Class

```csharp
var err = new Error("Not found")
    .WithCode("NOT_FOUND")
    .WithMetadata("id", 42);

// Predefined factories
var err = Error.NotFound("Product");
var err = Error.Validation("email", "Invalid email address");
var err = Error.Unauthorized();
var err = Error.Unexpected(exception);
var err = Error.Conflict("Name already taken");

// Error chain
var err = Error.NotFound("User")
    .WithCause(new Error("Database connection failed"));
```

---

## ValidationResult – collect multiple errors

```csharp
var result = Validator<RegisterDto>.For(dto)
    .Must(d => d.Name.Length >= 2,    Error.Validation("name",  "At least 2 characters"))
    .Must(d => d.Email.Contains('@'), Error.Validation("email", "Must be a valid email"))
    .Must(d => d.Age >= 18,           Error.Validation("age",   "Must be at least 18"))
    .Validate();

result.Match(
    onSuccess: dto    => Console.WriteLine($"Registered: {dto.Name}"),
    onFailure: errors => errors.ToList().ForEach(e => Console.WriteLine($"  x {e.Message}"))
);
```

---

## Railway Pipelines

### Synchronous

```csharp
var token = Pipeline<string>
    .Start(input)
    .Validate(s => s.Length > 0, "Must not be empty")
    .Then(s => FindUser(s))
    .Tap(user => logger.LogInformation("Login: {Name}", user.Name))
    .Then(user => CreateToken(user))
    .Finally(
        onSuccess: t   => $"Token: {t}",
        onFailure: err => $"Error: {err.Message}"
    );
```

### Asynchronous

```csharp
var result = await AsyncPipeline<Order>
    .Start(() => LoadOrderAsync(id))
    .Validate(order => order.Items.Count > 0, "Order must contain at least one item")
    .ThenAsync(order => ReserveStockAsync(order))
    .Tap(order => logger.LogInformation("Stock reserved: {Id}", order.Id))
    .ThenAsync(order => ProcessPaymentAsync(order))
    .TapAsync(async order => await SendConfirmationAsync(order))
    .Finally(
        onSuccess: _ => "Order placed successfully!",
        onFailure: e => $"Error: {e.Message}"
    );
```

---

## ASP.NET Core Integration

```bash
dotnet add package Resulta.AspNetCore
```

```csharp
builder.Services.AddResulta();
app.UseResulta();

// MVC
[HttpGet("{id}")]
public IActionResult Get(int id)
    => _service.GetUser(id).ToActionResult(this);

// Minimal API
app.MapGet("/api/users/{id}", (int id, UserService svc)
    => svc.GetUser(id).ToMinimalApiResult());

// Minimal API with TypedResults + OpenAPI annotations
app.MapGet("/api/users/{id}", (int id, UserService svc)
    => svc.GetUser(id).ToTypedResult())
   .ProducesResultaErrors();
```

| `Error.Code`       | HTTP Status               |
|--------------------|---------------------------|
| `NOT_FOUND`        | 404 Not Found             |
| `VALIDATION_ERROR` | 400 Bad Request           |
| `UNAUTHORIZED`     | 401 Unauthorized          |
| `CONFLICT`         | 409 Conflict              |
| _(anything else)_  | 500 Internal Server Error |

All failure responses use the RFC 7807 `application/problem+json` format. Validation errors return `HttpValidationProblemDetails` with an `errors` dictionary keyed by field name. Other codes return `ProblemDetails`. The original `Error.Code` is preserved on every response as the `code` extension property:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "'User' was not found.",
  "instance": "/api/users/42",
  "code": "NOT_FOUND"
}
```

For type-safe Minimal API endpoints with full OpenAPI metadata, use `ToTypedResult()` together with `ProducesResultaErrors()` — the endpoint then advertises every possible response shape (200/204, 404, 400 validation, 409, and `ProblemHttpResult` for 401/500).

### JSON converters for `Result` / `Error`

The core package also ships System.Text.Json converters if you want to serialize `Result<T>` over the wire (for inter-service messaging, queues, or persistence) instead of mapping through HTTP:

```csharp
var options = new JsonSerializerOptions().AddResultaConverters();
var json    = JsonSerializer.Serialize(Result<int>.Fail(Error.Validation("email", "Invalid")), options);
// → {"isSuccess":false,"error":{"message":"Validation failed for 'email': Invalid","code":"VALIDATION_ERROR","field":"email"}}
```

The converters never leak exception stack traces and truncate `causedBy` chains at three levels.

---

## FluentValidation Bridge

```bash
dotnet add package Resulta.FluentValidation
```

```csharp
public Result<User> Register(RegisterDto dto) =>
    _validator.ValidateToResult(dto).Bind(CreateUser);

public async Task<Result<User>> RegisterAsync(RegisterDto dto) =>
    await _validator.ValidateToResultAsync(dto).Bind(CreateUserAsync);
```

---

## Project Structure

```text
Resulta/
├── Resulta/
│   ├── src/
│   │   ├── Result.cs
│   │   ├── ResultT.cs
│   │   ├── Error.cs
│   │   └── ResultExtensions.cs
│   └── extensions/
│       ├── ValidationResult.cs
│       └── Pipeline.cs
├── Resulta.AspNetCore/
│   └── AspNetCoreIntegration.cs
├── Resulta.FluentValidation/
│   └── FluentValidationBridge.cs
├── samples/
│   └── Resulta.Samples/     # optional console demos (not packed)
├── Resulta.Tests/
├── .github/
│   └── workflows/
│       ├── ci.yml
│       └── release.yml
├── CHANGELOG.md
├── VERSIONING.md
└── README.md
```

---

## Releases and Versioning

Resulta follows **Semantic Versioning**:

- **MAJOR** for breaking changes
- **MINOR** for backwards-compatible features
- **PATCH** for fixes and small improvements

NuGet packages are published on every **MINOR** or **MAJOR** version bump.

For release history, see [CHANGELOG.md](./CHANGELOG.md).
For version bump rules and release guidance, see [VERSIONING.md](./VERSIONING.md).

---

## Contributing

Contributions, issues and feature requests are welcome!
Feel free to open an issue or submit a pull request on [GitHub](https://github.com/Kentarohakase/Resulta).

---

## License

MIT – see [LICENSE](LICENSE) for details.
