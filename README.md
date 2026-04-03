# ✨ Resulta

A lightweight C# library for the Result pattern – structured error handling without relying on exceptions for expected failures.

Stop throwing exceptions for expected failures. Start returning results.

---

## Documentation

- [Changelog](./CHANGELOG.md)
- [Versioning Guide](./VERSIONING.md)

These documents describe the release history and the versioning strategy used by Resulta.

---

## Packages

Resulta is split into separate packages:

- `Resulta` – core result types, validation primitives, helper methods, and pipelines
- `Resulta.AspNetCore` – ASP.NET Core integration
- `Resulta.FluentValidation` – FluentValidation integration

This keeps the core package lightweight while allowing optional integrations when needed.

---

## Why Resulta?

Using exceptions for expected outcomes often makes code harder to read, harder to test, and easier to misuse.

### Without Resulta

```csharp
try
{
    var user = _userService.GetUser(id);
    var order = _orderService.Place(user);
    return Ok(order);
}
catch (NotFoundException ex)
{
    return NotFound(ex.Message);
}
catch (ValidationException ex)
{
    return BadRequest(ex.Message);
}
catch (Exception ex)
{
    return StatusCode(500, ex.Message);
}
```

### With Resulta

```csharp
return _userService.GetUser(id)
    .Bind(user => _orderService.Place(user))
    .Match<IActionResult>(
        onSuccess: order => Ok(order),
        onFailure: err => err.Code switch
        {
            "NOT_FOUND"        => NotFound(err.Message),
            "VALIDATION_ERROR" => BadRequest(err.Message),
            _                  => StatusCode(500, err.Message)
        }
    );
```

Resulta makes success and failure explicit, composable, and type-safe.

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

`Resulta` contains the core result types and helpers.

`Resulta.AspNetCore` adds ASP.NET Core integration such as HTTP result mapping and middleware setup.

`Resulta.FluentValidation` adds integration helpers for converting FluentValidation results into `Result` and `ValidationResult<T>`.

---

## Quick Start

### Basic Ok and Fail

```csharp
using Resulta;

Result<int> Divide(int a, int b)
{
    if (b == 0)
        return Result.Fail<int>("Division by zero");

    return Result.Ok(a / b);
}

var result = Divide(10, 2);

if (result.IsSuccess)
    Console.WriteLine($"Result: {result.Value}");
else
    Console.WriteLine($"Error: {result.Error.Message}");
```

### Map, Bind, and Ensure

```csharp
var dto = LoadUser(1)
    .Bind(user => ValidateUser(user))
    .Ensure(user => user.IsActive, "User account is inactive")
    .Map(user => new UserDto(user.Name, user.Email));
```

### Match

```csharp
string message = result.Match(
    onSuccess: value => $"Success: {value}",
    onFailure: err   => $"Error [{err.Code}]: {err.Message}"
);
```

### Try and TryAsync

```csharp
var parsed = ResultExtensions.Try(
    () => int.Parse(userInput),
    ex => new Error($"Invalid number format: {ex.Message}").WithCode("PARSE_ERROR")
);
```

---

## Features

| Feature | Description |
|---|---|
| `Result` / `Result<T>` | Explicit success or failure without exceptions |
| `Error` | Structured error model with message, code, metadata, cause chain, and exception |
| `Map` / `Bind` | Transform and compose operations fluently |
| `Match` | Enforce handling for both success and failure |
| `Ensure` | Add inline validation to a successful result |
| `Try` / `TryAsync` | Convert exceptions into `Result` values |
| `Combine` | Merge multiple results into a single outcome |
| `ValidationResult<T>` | Collect multiple validation errors |
| `Validator<T>` | Fluent validation builder |
| `Pipeline<T>` / `AsyncPipeline<T>` | Railway-oriented composition for sync and async flows |
| `Resulta.AspNetCore` | Optional package for converting results into HTTP responses |
| `Resulta.FluentValidation` | Optional package for turning FluentValidation output into `Result` and `ValidationResult<T>` |

---

## ValidationResult

Collect multiple validation errors at once.

```csharp
var result = Validator<RegisterDto>.For(dto)
    .Must(d => d.Name.Length >= 2,    Error.Validation("name",  "At least 2 characters"))
    .Must(d => d.Email.Contains('@'), Error.Validation("email", "Must be a valid email"))
    .Must(d => d.Age >= 18,           Error.Validation("age",   "Must be at least 18"))
    .Validate();

result.Match(
    onSuccess: value => Console.WriteLine($"Welcome, {value.Name}!"),
    onFailure: errors =>
    {
        Console.WriteLine($"{errors.Count} validation error(s):");
        foreach (var error in errors)
            Console.WriteLine($" - {error.Message}");
    }
);
```

---

## Railway Pipelines

### Synchronous pipeline

```csharp
var token = Pipeline<string>
    .Start(loginRequest.Username)
    .Validate(s => !string.IsNullOrWhiteSpace(s), "Username must not be empty")
    .Then(FindUser)
    .Then(CheckRoles)
    .Tap(user => _logger.LogInformation("Login: {Name}", user.Name))
    .Then(CreateToken)
    .Finally(
        onSuccess: value => $"Bearer {value}",
        onFailure: err   => $"Login failed: {err.Message}"
    );
```

### Asynchronous pipeline

```csharp
var orderMessage = await AsyncPipeline<Order>
    .Start(() => LoadOrderAsync(orderId))
    .Then(ValidateOrder)
    .ThenAsync(ReserveStockAsync)
    .ThenAsync(ProcessPaymentAsync)
    .ThenAsync(SendConfirmationAsync)
    .Finally(
        onSuccess: _ => "Order placed successfully",
        onFailure: e => $"Order failed: {e.Message}"
    );
```

---

## ASP.NET Core Integration

Install the ASP.NET Core integration package:

```bash
dotnet add package Resulta.AspNetCore
```

Import the namespace:

```csharp
using Resulta.AspNetCore;
```

### Setup

```csharp
builder.Services.AddResulta();
app.UseResulta();
```

### MVC / Controllers

```csharp
[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly UserService _service;

    public UserController(UserService service)
    {
        _service = service;
    }

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
```

### Minimal API

```csharp
app.MapGet("/api/users/{id}", (int id, UserService service)
    => service.GetUser(id).ToMinimalApiResult());
```

### Default HTTP mapping

| Error code | HTTP status |
|---|---|
| `NOT_FOUND` | `404 Not Found` |
| `VALIDATION_ERROR` | `400 Bad Request` |
| `UNAUTHORIZED` | `401 Unauthorized` |
| `CONFLICT` | `409 Conflict` |
| any other code | `500 Internal Server Error` |

---

## FluentValidation Integration

Install the FluentValidation integration package:

```bash
dotnet add package Resulta.FluentValidation
```

Import the namespace:

```csharp
using Resulta.FluentValidation;
```

### Validator example

```csharp
public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MinimumLength(2);
        RuleFor(x => x.Email).EmailAddress();
        RuleFor(x => x.Age).GreaterThanOrEqualTo(18);
    }
}
```

### Convert validation output into `Result`

```csharp
public Result<User> Register(RegisterDto dto) =>
    _validator
        .ValidateToResult(dto)
        .Bind(CreateUser);
```

### Async variant

```csharp
public async Task<Result<User>> RegisterAsync(RegisterDto dto) =>
    await _validator
        .ValidateToResultAsync(dto)
        .Bind(CreateUserAsync);
```

### Convert validation output into `ValidationResult<T>`

```csharp
var validationResult = await _validator.ToValidationResultAsync(dto);

validationResult.Match(
    onSuccess: value => "Validation passed",
    onFailure: errors => $"Validation failed with {errors.Count} error(s)"
);
```

---

## Project Structure

```text
Resulta/
├── Resulta/
│   ├── Result.cs
│   ├── ResultT.cs
│   ├── Error.cs
│   ├── ResultExtensions.cs
│   ├── ValidationResult.cs
│   └── Pipeline.cs
├── Resulta.AspNetCore/
│   └── AspNetCoreIntegration.cs
├── Resulta.FluentValidation/
│   └── FluentValidationBridge.cs
├── Resulta.Tests/
├── CHANGELOG.md
├── VERSIONING.md
├── README.md
└── Resulta.slnx
```

---

## Releases and Versioning

Resulta follows **Semantic Versioning**:

- **MAJOR** for breaking changes
- **MINOR** for backwards-compatible features
- **PATCH** for fixes and small improvements

For release history, see [CHANGELOG.md](./CHANGELOG.md).  
For version bump rules and release guidance, see [VERSIONING.md](./VERSIONING.md).

---

## Contributing

Contributions, issues, and feature requests are welcome.

1. Fork the repository
2. Create a feature branch  
   `git checkout -b feature/my-feature`
3. Commit your changes  
   `git commit -m "Add my feature"`
4. Push your branch  
   `git push origin feature/my-feature`
5. Open a Pull Request

---

## License

MIT © Kentaro

See [LICENSE.txt](./LICENSE.txt) for details.

---

If you find Resulta useful, consider giving the repository a star.