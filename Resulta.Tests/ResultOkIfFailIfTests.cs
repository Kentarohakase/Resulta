using Resulta;

using Xunit;

namespace Resulta.Tests;

public sealed class ResultOkIfFailIfTests
{
  // ── OkIf (non-generic) ───────────────────────────────────────────────────

  [Fact]
  public void OkIf_Should_Return_Ok_When_Condition_Is_True()
  {
    var result = Result.OkIf(true, "Fehler");

    Assert.True(result.IsSuccess);
  }

  [Fact]
  public void OkIf_Should_Return_Fail_When_Condition_Is_False()
  {
    var result = Result.OkIf(false, "Bedingung nicht erfüllt");

    Assert.True(result.IsFailure);
    Assert.Equal("Bedingung nicht erfüllt", result.Error.Message);
  }

  [Fact]
  public void OkIf_With_Error_Object_Should_Return_Fail_With_That_Error()
  {
    var error = Error.Unauthorized("Kein Zugriff");
    var result = Result.OkIf(false, error);

    Assert.True(result.IsFailure);
    Assert.Equal("UNAUTHORIZED", result.Error.Code);
  }

  // ── FailIf (non-generic) ─────────────────────────────────────────────────

  [Fact]
  public void FailIf_Should_Return_Fail_When_Condition_Is_True()
  {
    var result = Result.FailIf(true, "Bedingung eingetreten");

    Assert.True(result.IsFailure);
    Assert.Equal("Bedingung eingetreten", result.Error.Message);
  }

  [Fact]
  public void FailIf_Should_Return_Ok_When_Condition_Is_False()
  {
    var result = Result.FailIf(false, "Fehler");

    Assert.True(result.IsSuccess);
  }

  [Fact]
  public void FailIf_With_Error_Object_Should_Return_Fail_With_That_Error()
  {
    var error = Error.Conflict("Bereits vorhanden");
    var result = Result.FailIf(true, error);

    Assert.True(result.IsFailure);
    Assert.Equal("CONFLICT", result.Error.Code);
  }

  // ── OkIf (generic) ───────────────────────────────────────────────────────

  [Fact]
  public void OkIf_Generic_Should_Return_Ok_With_Value_When_Condition_Is_True()
  {
    var result = Result.OkIf(true, 42, "Fehler");

    Assert.True(result.IsSuccess);
    Assert.Equal(42, result.Value);
  }

  [Fact]
  public void OkIf_Generic_Should_Return_Fail_When_Condition_Is_False()
  {
    var result = Result.OkIf(false, 42, "Bedingung nicht erfüllt");

    Assert.True(result.IsFailure);
    Assert.Equal("Bedingung nicht erfüllt", result.Error.Message);
  }

  [Fact]
  public void OkIf_Generic_With_Error_Object_Should_Return_Fail_With_That_Error()
  {
    var error = Error.NotFound("User");
    var result = Result.OkIf(false, 42, error);

    Assert.True(result.IsFailure);
    Assert.Equal("NOT_FOUND", result.Error.Code);
  }

  // ── FailIf (generic) ─────────────────────────────────────────────────────

  [Fact]
  public void FailIf_Generic_Should_Return_Fail_When_Condition_Is_True()
  {
    var result = Result.FailIf(true, 42, "Bedingung eingetreten");

    Assert.True(result.IsFailure);
    Assert.Equal("Bedingung eingetreten", result.Error.Message);
  }

  [Fact]
  public void FailIf_Generic_Should_Return_Ok_With_Value_When_Condition_Is_False()
  {
    var result = Result.FailIf(false, 42, "Fehler");

    Assert.True(result.IsSuccess);
    Assert.Equal(42, result.Value);
  }

  [Fact]
  public void FailIf_Generic_With_Error_Object_Should_Return_Fail_With_That_Error()
  {
    var error = Error.Conflict("Bereits vorhanden");
    var result = Result.FailIf(true, "wert", error);

    Assert.True(result.IsFailure);
    Assert.Equal("CONFLICT", result.Error.Code);
  }

  // ── Practical usage ───────────────────────────────────────────────────────

  [Fact]
  public void OkIf_Can_Be_Chained_With_Map()
  {
    var isActive = true;

    var result = Result.OkIf(isActive, "User", "User ist nicht aktiv")
        .Map(name => name.ToUpperInvariant());

    Assert.True(result.IsSuccess);
    Assert.Equal("USER", result.Value);
  }

  [Fact]
  public void FailIf_Can_Be_Chained_With_Bind()
  {
    var exists = false;

    var result = Result.FailIf<int>(exists, 0, Error.Conflict("Bereits vorhanden"))
        .Bind(x => Result.Ok(x + 1));

    Assert.True(result.IsSuccess);
    Assert.Equal(1, result.Value);
  }
}