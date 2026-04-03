<div align="center">

# ✨ Resulta

**A lightweight C# library for the Result pattern – error handling without exceptions.**

[![NuGet](https://img.shields.io/nuget/v/Resulta?color=blue&logo=nuget)](https://www.nuget.org/packages/Resulta)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Resulta?color=green)](https://www.nuget.org/packages/Resulta)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com)
[![Build](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/kentaro/Resulta)

<br/>

*Stop throwing exceptions for expected failures. Start returning Results.*

## Documentation

- [Changelog](./CHANGELOG.md)
- [Versioning Guide](./VERSIONING.md)

These documents describe release history and the versioning strategy used by Resulta.

</div>

---

## 🤔 The Problem

```csharp
// ❌ Exceptions as control flow – messy, slow, easy to forget
try
{
    var user = _userService.GetUser(id);   // throws NotFoundException?
    var order = _orderService.Place(user); // throws ValidationException?
    return Ok(order);
}
catch (NotFoundException ex)   { return NotFound(ex.Message); }
catch (ValidationException ex) { return BadRequest(ex.Message); }
catch (Exception ex)           { return StatusCode(500, ex.Message); }
```

## ✅ The Solution

```csharp
// ✅ Explicit, readable, type-safe – no surprises
return _userService.GetUser(id)
    .Bind(user => _orderService.Place(user))
    .Match<IActionResult>(
        onSuccess: order => Ok(order),
        onFailure: err   => err.Code switch
        {
            "NOT_FOUND"        => NotFound(err.Message),
            "VALIDATION_ERROR" => BadRequest(err.Message),
            _                  => StatusCode(500, err.Message)
        }
    );
```

---

## 📦 Installation

```bash
dotnet add package Resulta
```

---

## 🚀 Quick Start

### Basic Ok & Fail

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
    Console.WriteLine($"Result: {result.Value}");  // Result: 5
else
    Console.WriteLine($"Error: {result.Error}");
```

### Map, Bind & Chain

```csharp
var dto = LoadUser(1)                                           // Result<User>
    .Bind(user => ValidateUser(user))                          // Result<User>
    .Ensure(user => user.IsActive, "User account is inactive") // Result<User>
    .Map(user => new UserDto(user.Name, user.Email));          // Result<UserDto>
```

### Match – force handling of both cases

```csharp
string message = result.Match(
    onSuccess: value => $"✅ Success: {value}",
    onFailure: err   => $"❌ Error [{err.Code}]: {err.Message}"
);
```

### Try – wrap exceptions

```csharp
var result = ResultExtensions.Try(
    () => int.Parse(userInput),
    ex  => new Error("Invalid number format").WithCode("PARSE_ERROR")
);
```

---

## 🧩 Features

| Feature | Description |
|---------|-------------|
| `Result<T>` | Type-safe result with value or error |
| `Error` | Structured error with code, metadata & cause chain |
| `Map / Bind` | Transform and chain operations |
| `Match` | Force handling of both success & failure |
| `Ensure` | Validate values inline |
| `Try / TryAsync` | Wrap exceptions into Results |
| `Combine` | Merge multiple Results into one |
| `ValidationResult<T>` | Collect multiple errors (perfect for forms) |
| `Pipeline<T>` | Railway-Oriented pipeline (sync & async) |
| `ASP.NET Core` | Automatic Result → HTTP response mapping |
| `FluentValidation` | Bridge to FluentValidation |

---

## 📋 ValidationResult – collect multiple errors

```csharp
var result = Validator<RegisterDto>.For(dto)
    .Must(d => d.Name.Length >= 2,    Error.Validation("name",  "At least 2 characters"))
    .Must(d => d.Email.Contains('@'), Error.Validation("email", "Must be a valid email"))
    .Must(d => d.Age >= 18,           Error.Validation("age",   "Must be at least 18"))
    .Validate();

result.Match(
    onSuccess: dto    => Console.WriteLine($"Welcome, {dto.Name}!"),
    onFailure: errors =>
    {
        Console.WriteLine($"{errors.Count} validation error(s):");
        foreach (var e in errors)
            Console.WriteLine($"  • {e.Message}");
    }
);
```

---

## 🚂 Railway Pipeline

```csharp
// Sync
var token = Pipeline<string>
    .Start(loginRequest.Username)
    .Validate(s => s.Length > 0,    "Username must not be empty")
    .Then(s => FindUser(s))         // returns Result<User>
    .Then(user => CheckRoles(user)) // returns Result<User>
    .Tap(user => _logger.LogInformation("Login: {Name}", user.Name))
    .Then(user => CreateToken(user))
    .Finally(
        onSuccess: token => $"Bearer {token}",
        onFailure: err   => $"Login failed: {err.Message}"
    );

// Async
var order = await AsyncPipeline<Order>
    .Start(() => LoadOrderAsync(orderId))
    .Then(o => ValidateOrder(o))
    .ThenAsync(o => ReserveStockAsync(o))
    .ThenAsync(o => ProcessPaymentAsync(o))
    .ThenAsync(o => SendConfirmationAsync(o))
    .Finally(
        onSuccess: _ => "🎉 Order placed!",
        onFailure: e => $"Order failed: {e.Message}"
    );
```

---

## 🌐 ASP.NET Core Integration

```csharp
// Program.cs
builder.Services.AddResulta();
app.UseResulta(); // Global exception handling → structured JSON errors

// Controller – zero try/catch needed
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly UserService _service;

    [HttpGet("{id}")]
    public IActionResult Get(int id)
        => _service.GetUser(id).ToActionResult(this);

    [HttpPost]
    public IActionResult Create(CreateUserDto dto)
        => _service.CreateUser(dto).ToActionResult(this);

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
        => _service.DeleteUser(id).ToActionResult(this);
}

// Minimal API
app.MapGet("/api/users/{id}", (int id, UserService svc)
    => svc.GetUser(id).ToMinimalApiResult());
```

**Automatic HTTP status code mapping:**

| `Error.Code` | HTTP Status |
|---|---|
| `NOT_FOUND` | `404 Not Found` |
| `VALIDATION_ERROR` | `400 Bad Request` |
| `UNAUTHORIZED` | `401 Unauthorized` |
| `CONFLICT` | `409 Conflict` |
| _(anything else)_ | `500 Internal Server Error` |

---

## 🔗 FluentValidation Bridge

```csharp
// dotnet add package FluentValidation

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2);
        RuleFor(x => x.Email).EmailAddress();
        RuleFor(x => x.Age).GreaterThanOrEqualTo(18);
    }
}

// In your service – returns Result directly
public Result<User> Register(RegisterDto dto) =>
    _validator.ValidateToResult(dto)
              .Bind(CreateUser);

// Async
public async Task<Result<User>> RegisterAsync(RegisterDto dto) =>
    await _validator.ValidateToResultAsync(dto)
                    .Bind(CreateUserAsync);
```

---

## 🗂️ Project Structure

```
Resulta/
├── src/
│   ├── Result.cs                  # Non-generic Result
│   ├── ResultT.cs                 # Result<T> with Map, Bind, Match
│   ├── Error.cs                   # Structured error with code & metadata
│   └── ResultExtensions.cs        # Async, Try, Combine, Ensure
└── extensions/
    ├── ValidationResult.cs        # Multiple validation errors
    ├── Pipeline.cs                # Railway-Oriented Pipeline
    ├── AspNetCoreIntegration.cs   # HTTP middleware & helpers
    └── FluentValidationBridge.cs  # FluentValidation integration
```

---

## 🤝 Contributing

Contributions, issues and feature requests are welcome!

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request

---

## 📄 License

MIT © [Kentaro](https://github.com/kentaro) – see [LICENSE](LICENSE) for details.

---

<div align="center">

⭐ **If you find Resulta useful, please consider giving it a star!** ⭐

</div>
