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

[HttpGet("{id}")]
public IActionResult Get(int id)
    => _service.GetUser(id).ToActionResult(this);

app.MapGet("/api/users/{id}", (int id, UserService svc)
    => svc.GetUser(id).ToMinimalApiResult());
```

| `Error.Code`       | HTTP Status               |
|--------------------|---------------------------|
| `NOT_FOUND`        | 404 Not Found             |
| `VALIDATION_ERROR` | 400 Bad Request           |
| `UNAUTHORIZED`     | 401 Unauthorized          |
| `CONFLICT`         | 409 Conflict              |
| _(anything else)_  | 500 Internal Server Error |

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

## Contributing

Contributions, issues and feature requests are welcome!
Feel free to open an issue or submit a pull request on [GitHub](https://github.com/Kentarohakase/Resulta).

---

## License

MIT – see [LICENSE](LICENSE) for details.
