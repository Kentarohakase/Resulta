using System.Threading.Tasks;

using FluentValidation;

using Resulta;
using Resulta.Extensions;
using Resulta.FluentValidation;

using Xunit;

namespace Resulta.Tests;

// ── Test DTOs & Validators ────────────────────────────────────────────────────

internal sealed record RegisterDto(string Name, string Email, int Age);

internal sealed class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
  public RegisterDtoValidator()
  {
    RuleFor(x => x.Name)
        .NotEmpty().WithMessage("Name darf nicht leer sein")
        .MinimumLength(2).WithMessage("Name muss mindestens 2 Zeichen lang sein");

    RuleFor(x => x.Email)
        .NotEmpty().WithMessage("E-Mail darf nicht leer sein")
        .EmailAddress().WithMessage("E-Mail muss eine gültige Adresse sein");

    RuleFor(x => x.Age)
        .GreaterThanOrEqualTo(18).WithMessage("Muss mindestens 18 Jahre alt sein");
  }
}

// ── ValidateToResult (sync) ───────────────────────────────────────────────────

public sealed class FluentValidationBridge_ValidateToResultTests
{
  private readonly RegisterDtoValidator _validator = new();

  [Fact]
  public void ValidateToResult_Should_Return_Ok_When_All_Rules_Pass()
  {
    var dto = new RegisterDto("Kentaro", "kentaro@example.com", 25);
    var result = _validator.ValidateToResult(dto);
    Assert.True(result.IsSuccess);
    Assert.Equal(dto, result.Value);
  }

  [Fact]
  public void ValidateToResult_Should_Return_Fail_When_A_Rule_Fails()
  {
    var dto = new RegisterDto("", "kentaro@example.com", 25);
    var result = _validator.ValidateToResult(dto);
    Assert.True(result.IsFailure);
    Assert.Equal("VALIDATION_ERROR", result.Error.Code);
  }

  [Fact]
  public void ValidateToResult_Should_Chain_Multiple_Errors_As_Causes()
  {
    var dto = new RegisterDto("", "ungueltig", 16);
    var result = _validator.ValidateToResult(dto);
    Assert.True(result.IsFailure);
    Assert.NotNull(result.Error.CausedBy);
  }

  [Fact]
  public void ValidateToResult_Should_Include_AttemptedValue_In_Metadata()
  {
    var dto = new RegisterDto("", "kentaro@example.com", 25);
    var result = _validator.ValidateToResult(dto);
    Assert.True(result.IsFailure);
    Assert.True(result.Error.Metadata.ContainsKey("attemptedValue"));
  }

  [Fact]
  public void ValidateToResult_Should_Include_Severity_In_Metadata()
  {
    var dto = new RegisterDto("", "kentaro@example.com", 25);
    var result = _validator.ValidateToResult(dto);
    Assert.True(result.IsFailure);
    Assert.True(result.Error.Metadata.ContainsKey("severity"));
  }

  [Fact]
  public void ValidateToResult_Can_Be_Chained_With_Bind()
  {
    var dto = new RegisterDto("Kentaro", "kentaro@example.com", 25);
    var result = _validator
        .ValidateToResult(dto)
        .Bind(d => Result.Ok(d.Name.ToUpperInvariant()));
    Assert.True(result.IsSuccess);
    Assert.Equal("KENTARO", result.Value);
  }

  [Fact]
  public void ValidateToResult_Bind_Should_Propagate_Failure()
  {
    var dto = new RegisterDto("", "ungueltig", 16);
    var result = _validator
        .ValidateToResult(dto)
        .Bind(d => Result.Ok(d.Name.ToUpperInvariant()));
    Assert.True(result.IsFailure);
    Assert.Equal("VALIDATION_ERROR", result.Error.Code);
  }
}

// ── ToResult (extension on FVResult) ─────────────────────────────────────────

public sealed class FluentValidationBridge_ToResultTests
{
  private readonly RegisterDtoValidator _validator = new();

  [Fact]
  public void ToResult_Should_Return_Ok_When_Validation_Passes()
  {
    var dto = new RegisterDto("Kentaro", "kentaro@example.com", 25);
    var fvResult = _validator.Validate(dto);
    var result = fvResult.ToResult(dto);
    Assert.True(result.IsSuccess);
    Assert.Equal(dto, result.Value);
  }

  [Fact]
  public void ToResult_Should_Return_Fail_When_Validation_Fails()
  {
    var dto = new RegisterDto("", "ungueltig", 16);
    var fvResult = _validator.Validate(dto);
    var result = fvResult.ToResult(dto);
    Assert.True(result.IsFailure);
    Assert.Equal("VALIDATION_ERROR", result.Error.Code);
  }
}

// ── ToValidationResult (sync) ─────────────────────────────────────────────────

public sealed class FluentValidationBridge_ToValidationResultTests
{
  private readonly RegisterDtoValidator _validator = new();

  [Fact]
  public void ToValidationResult_Should_Return_Ok_When_All_Rules_Pass()
  {
    var dto = new RegisterDto("Kentaro", "kentaro@example.com", 25);
    var result = _validator.ToValidationResult(dto);
    Assert.True(result.IsSuccess);
    Assert.Equal(dto, result.Value);
    Assert.Empty(result.Errors);
  }

  [Fact]
  public void ToValidationResult_Should_Return_All_Errors_When_Rules_Fail()
  {
    var dto = new RegisterDto("", "ungueltig", 16);
    var result = _validator.ToValidationResult(dto);
    Assert.True(result.IsFailure);
    Assert.True(result.Errors.Count >= 3);
  }

  [Fact]
  public void ToValidationResult_Should_Return_ValidationError_Code_For_Each_Error()
  {
    var dto = new RegisterDto("", "ungueltig", 16);
    var result = _validator.ToValidationResult(dto);
    Assert.All(result.Errors, e => Assert.Equal("VALIDATION_ERROR", e.Code));
  }

  [Fact]
  public void ToValidationResult_Should_Include_AttemptedValue_In_Metadata()
  {
    var dto = new RegisterDto("", "kentaro@example.com", 25);
    var result = _validator.ToValidationResult(dto);
    Assert.True(result.IsFailure);
    Assert.All(result.Errors, e => Assert.True(e.Metadata.ContainsKey("attemptedValue")));
  }

  [Fact]
  public void ToValidationResult_Can_Be_Converted_To_Result()
  {
    var dto = new RegisterDto("Kentaro", "kentaro@example.com", 25);
    var result = _validator.ToValidationResult(dto).ToResult();
    Assert.True(result.IsSuccess);
  }

  [Fact]
  public void ToValidationResult_ToResult_Should_Propagate_Failure()
  {
    var dto = new RegisterDto("", "ungueltig", 16);
    var result = _validator.ToValidationResult(dto).ToResult();
    Assert.True(result.IsFailure);
    Assert.Equal("VALIDATION_ERROR", result.Error.Code);
  }
}

// ── Async ─────────────────────────────────────────────────────────────────────

public sealed class FluentValidationBridge_AsyncTests
{
  private readonly RegisterDtoValidator _validator = new();

  [Fact]
  public async Task ValidateToResultAsync_Should_Return_Ok_When_All_Rules_Pass()
  {
    var dto = new RegisterDto("Kentaro", "kentaro@example.com", 25);
    var result = await _validator.ValidateToResultAsync(dto);
    Assert.True(result.IsSuccess);
    Assert.Equal(dto, result.Value);
  }

  [Fact]
  public async Task ValidateToResultAsync_Should_Return_Fail_When_A_Rule_Fails()
  {
    var dto = new RegisterDto("", "ungueltig", 16);
    var result = await _validator.ValidateToResultAsync(dto);
    Assert.True(result.IsFailure);
    Assert.Equal("VALIDATION_ERROR", result.Error.Code);
  }

  [Fact]
  public async Task ValidateToResultAsync_Can_Be_Chained_With_Bind()
  {
    var dto = new RegisterDto("Kentaro", "kentaro@example.com", 25);
    var result = await _validator.ValidateToResultAsync(dto);
    var bound = result.Bind(d => Result.Ok(d.Name.ToUpperInvariant()));
    Assert.True(bound.IsSuccess);
    Assert.Equal("KENTARO", bound.Value);
  }

  [Fact]
  public async Task ToValidationResultAsync_Should_Return_Ok_When_All_Rules_Pass()
  {
    var dto = new RegisterDto("Kentaro", "kentaro@example.com", 25);
    var result = await _validator.ToValidationResultAsync(dto);
    Assert.True(result.IsSuccess);
    Assert.Equal(dto, result.Value);
    Assert.Empty(result.Errors);
  }

  [Fact]
  public async Task ToValidationResultAsync_Should_Return_All_Errors_When_Rules_Fail()
  {
    var dto = new RegisterDto("", "ungueltig", 16);
    var result = await _validator.ToValidationResultAsync(dto);
    Assert.True(result.IsFailure);
    Assert.True(result.Errors.Count >= 3);
  }

  [Fact]
  public async Task ToValidationResultAsync_Should_Return_ValidationError_Code_For_Each_Error()
  {
    var dto = new RegisterDto("", "ungueltig", 16);
    var result = await _validator.ToValidationResultAsync(dto);
    Assert.All(result.Errors, e => Assert.Equal("VALIDATION_ERROR", e.Code));
  }

  [Fact]
  public async Task ToValidationResultAsync_Match_Should_Call_Success_Branch_On_Ok()
  {
    var dto = new RegisterDto("Kentaro", "kentaro@example.com", 25);
    var result = await _validator.ToValidationResultAsync(dto);
    var message = result.Match(
        onSuccess: d => $"Willkommen, {d.Name}!",
        onFailure: errors => $"{errors.Count} Fehler"
    );
    Assert.Equal("Willkommen, Kentaro!", message);
  }

  [Fact]
  public async Task ToValidationResultAsync_Match_Should_Call_Failure_Branch_On_Fail()
  {
    var dto = new RegisterDto("", "ungueltig", 16);
    var result = await _validator.ToValidationResultAsync(dto);
    var message = result.Match(
        onSuccess: d => $"Willkommen, {d.Name}!",
        onFailure: errors => $"{errors.Count} Fehler"
    );
    Assert.Contains("Fehler", message);
  }
}