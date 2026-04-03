using System;
using System.Linq;

using Resulta;
using Resulta.Extensions;
using Xunit;

namespace Resulta.Tests;

public sealed class ValidationResultTests
{
  [Fact]
  public void Ok_Should_Create_Success_ValidationResult()
  {
    var result = ValidationResult<int>.Ok(42);

    Assert.True(result.IsSuccess);
    Assert.False(result.IsFailure);
    Assert.Equal(42, result.Value);
    Assert.Empty(result.Errors);
  }

  [Fact]
  public void Fail_With_Single_Error_Should_Create_Failure_ValidationResult()
  {
    var error = Error.Validation("name", "Name ist erforderlich");
    var result = ValidationResult<string>.Fail(error);

    Assert.False(result.IsSuccess);
    Assert.True(result.IsFailure);
    Assert.Single(result.Errors);
    Assert.Equal("VALIDATION_ERROR", result.Error.Code);
    Assert.Contains("name", result.Error.Message);
    Assert.Contains("Name ist erforderlich", result.Error.Message);
  }

  [Fact]
  public void Fail_With_Multiple_Errors_Should_Contain_All_Errors()
  {
    var errors = new[]
    {
            Error.Validation("name", "Name ist erforderlich"),
            Error.Validation("email", "E-Mail ist ungültig")
        };

    var result = ValidationResult<string>.Fail(errors);

    Assert.True(result.IsFailure);
    Assert.Equal(2, result.Errors.Count);
    Assert.Equal("VALIDATION_ERROR", result.Errors[0].Code);
    Assert.Equal("VALIDATION_ERROR", result.Errors[1].Code);

    Assert.Contains("name", result.Errors[0].Message);
    Assert.Contains("Name ist erforderlich", result.Errors[0].Message);

    Assert.Contains("email", result.Errors[1].Message);
    Assert.Contains("E-Mail ist ungültig", result.Errors[1].Message);
  }

  [Fact]
  public void Value_Should_Throw_When_Validation_Failed()
  {
    var result = ValidationResult<string>.Fail(
        Error.Validation("name", "Name ist erforderlich"));

    var ex = Assert.Throws<InvalidOperationException>(() => _ = result.Value);

    Assert.Contains("validation has failed", ex.Message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void Error_Should_Return_First_Error()
  {
    var first = Error.Validation("name", "Name ist erforderlich");
    var second = Error.Validation("email", "E-Mail ist ungültig");

    var result = ValidationResult<string>.Fail(first, second);

    Assert.Equal(first.Message, result.Error.Message);
    Assert.Equal(first.Code, result.Error.Code);
  }

  [Fact]
  public void Match_Should_Call_Success_Branch_When_Result_Is_Ok()
  {
    var result = ValidationResult<int>.Ok(10);

    var text = result.Match(
        onSuccess: value => $"Wert: {value}",
        onFailure: errors => $"Fehler: {errors.Count}"
    );

    Assert.Equal("Wert: 10", text);
  }

  [Fact]
  public void Match_Should_Call_Failure_Branch_When_Result_Is_Failed()
  {
    var result = ValidationResult<int>.Fail(
        Error.Validation("age", "Muss mindestens 18 sein"),
        Error.Validation("email", "E-Mail ist ungültig"));

    var text = result.Match(
        onSuccess: value => $"Wert: {value}",
        onFailure: errors => $"Fehler: {errors.Count}"
    );

    Assert.Equal("Fehler: 2", text);
  }

  [Fact]
  public void ToResult_Should_Return_Success_Result_When_Validation_Is_Ok()
  {
    var validation = ValidationResult<int>.Ok(99);

    var result = validation.ToResult();

    Assert.True(result.IsSuccess);
    Assert.Equal(99, result.Value);
  }

  [Fact]
  public void ToResult_Should_Return_Failure_Result_When_Validation_Fails()
  {
    var validation = ValidationResult<string>.Fail(
        Error.Validation("name", "Name ist erforderlich"),
        Error.Validation("email", "E-Mail ist ungültig"));

    var result = validation.ToResult();

    Assert.True(result.IsFailure);
    Assert.Equal("VALIDATION_ERROR", result.Error.Code);
    Assert.Contains("name", result.Error.Message);
    Assert.Contains("Name ist erforderlich", result.Error.Message);
  }

  [Fact]
  public void ToString_Should_Contain_Ok_For_Success()
  {
    var result = ValidationResult<int>.Ok(1);

    var text = result.ToString();

    Assert.Contains("Ok", text);
  }

  [Fact]
  public void ToString_Should_Contain_Error_Count_For_Failure()
  {
    var result = ValidationResult<int>.Fail(
        Error.Validation("a", "Fehler A"),
        Error.Validation("b", "Fehler B"));

    var text = result.ToString();

    Assert.Contains("2 error(s)", text);
  }

  [Fact]
  public void Fail_With_Empty_Error_Array_Should_Throw()
  {
    var ex = Assert.Throws<ArgumentException>(() => ValidationResult<int>.Fail(Array.Empty<Error>()));

    Assert.Contains("must contain at least one error", ex.Message, StringComparison.OrdinalIgnoreCase);
  }

  [Fact]
  public void Fail_With_Null_Enumerable_Should_Throw()
  {
    Assert.Throws<ArgumentNullException>(() => ValidationResult<int>.Fail((System.Collections.Generic.IEnumerable<Error>)null!));
  }
}

public sealed class ValidatorTests
{
  private sealed record RegisterDto(string Name, string Email, int Age);

  [Fact]
  public void Validate_Should_Return_Ok_When_All_Rules_Pass()
  {
    var dto = new RegisterDto("Kentaro", "kentaro@example.com", 25);

    var result = Validator<RegisterDto>
        .For(dto)
        .Must(x => !string.IsNullOrWhiteSpace(x.Name), "Name ist erforderlich")
        .Must(x => x.Email.Contains("@"), "E-Mail ist ungültig")
        .Must(x => x.Age >= 18, "Muss mindestens 18 sein")
        .Validate();

    Assert.True(result.IsSuccess);
    Assert.Equal(dto, result.Value);
    Assert.Empty(result.Errors);
  }

  [Fact]
  public void Validate_Should_Collect_Multiple_Errors_When_Rules_Fail()
  {
    var dto = new RegisterDto("", "ungueltig", 16);

    var result = Validator<RegisterDto>
        .For(dto)
        .Must(x => !string.IsNullOrWhiteSpace(x.Name), "Name ist erforderlich")
        .Must(x => x.Email.Contains("@"), "E-Mail ist ungültig")
        .Must(x => x.Age >= 18, "Muss mindestens 18 sein")
        .Validate();

    Assert.True(result.IsFailure);
    Assert.Equal(3, result.Errors.Count);
    Assert.Contains(result.Errors, e => e.Message == "Name ist erforderlich");
    Assert.Contains(result.Errors, e => e.Message == "E-Mail ist ungültig");
    Assert.Contains(result.Errors, e => e.Message == "Muss mindestens 18 sein");
  }

  [Fact]
  public void Must_With_Error_Object_Should_Add_That_Error()
  {
    var dto = new RegisterDto("", "test@example.com", 20);

    var result = Validator<RegisterDto>
        .For(dto)
        .Must(x => !string.IsNullOrWhiteSpace(x.Name),
            Error.Validation("name", "Name ist erforderlich"))
        .Validate();

    Assert.True(result.IsFailure);
    Assert.Single(result.Errors);
    Assert.Equal("VALIDATION_ERROR", result.Error.Code);
    Assert.Contains("name", result.Error.Message);
    Assert.Contains("Name ist erforderlich", result.Error.Message);
  }

  [Fact]
  public void MustNot_Should_Add_Error_When_Predicate_Is_True()
  {
    var dto = new RegisterDto("Kentaro", "kentaro@example.com", 16);

    var result = Validator<RegisterDto>
        .For(dto)
        .MustNot(x => x.Age < 18, "Muss mindestens 18 sein")
        .Validate();

    Assert.True(result.IsFailure);
    Assert.Single(result.Errors);
    Assert.Equal("Muss mindestens 18 sein", result.Error.Message);
  }

  [Fact]
  public void Must_Should_Not_Add_Error_When_Predicate_Is_True()
  {
    var dto = new RegisterDto("Kentaro", "kentaro@example.com", 25);

    var result = Validator<RegisterDto>
        .For(dto)
        .Must(x => x.Age >= 18, "Muss mindestens 18 sein")
        .Validate();

    Assert.True(result.IsSuccess);
  }

  [Fact]
  public void Must_With_Null_Predicate_Should_Throw()
  {
    var dto = new RegisterDto("Kentaro", "kentaro@example.com", 25);

    var validator = Validator<RegisterDto>.For(dto);

    Assert.Throws<ArgumentNullException>(() => validator.Must(null!, "Fehler"));
  }

  [Fact]
  public void Must_With_Null_Error_Should_Throw()
  {
    var dto = new RegisterDto("Kentaro", "kentaro@example.com", 25);

    var validator = Validator<RegisterDto>.For(dto);

    Assert.Throws<ArgumentNullException>(() => validator.Must(x => true, (Error)null!));
  }
}