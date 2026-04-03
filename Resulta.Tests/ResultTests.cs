using System;

using Resulta;

using Xunit;

namespace Resulta.Tests;

public sealed class ResultTests
{
  [Fact]
  public void Ok_Should_Create_Success_Result()
  {
    var result = Result.Ok();

    Assert.True(result.IsSuccess);
    Assert.False(result.IsFailure);
  }

  [Fact]
  public void Fail_With_Message_Should_Create_Failure_Result()
  {
    var result = Result.Fail("Fehler");

    Assert.False(result.IsSuccess);
    Assert.True(result.IsFailure);
    Assert.Equal("Fehler", result.Error.Message);
  }

  [Fact]
  public void Fail_With_Error_Should_Create_Failure_Result()
  {
    var error = new Error("Kaputt").WithCode("BROKEN");
    var result = Result.Fail(error);

    Assert.True(result.IsFailure);
    Assert.Equal("Kaputt", result.Error.Message);
    Assert.Equal("BROKEN", result.Error.Code);
  }

  [Fact]
  public void Match_Should_Call_Success_Branch_For_Ok_Result()
  {
    var result = Result.Ok();

    var value = result.Match(
        onSuccess: () => "OK",
        onFailure: _ => "FAIL"
    );

    Assert.Equal("OK", value);
  }

  [Fact]
  public void Match_Should_Call_Failure_Branch_For_Failed_Result()
  {
    var result = Result.Fail("Fehler");

    var value = result.Match(
        onSuccess: () => "OK",
        onFailure: err => err.Message
    );

    Assert.Equal("Fehler", value);
  }

  [Fact]
  public void OnSuccess_Should_Execute_Action_Only_For_Success()
  {
    var called = false;

    Result.Ok()
        .OnSuccess(() => called = true)
        .OnFailure(_ => throw new Exception("Should not be called"));

    Assert.True(called);
  }

  [Fact]
  public void OnFailure_Should_Execute_Action_Only_For_Failure()
  {
    var called = false;

    Result.Fail("Fehler")
        .OnSuccess(() => throw new Exception("Should not be called"))
        .OnFailure(_ => called = true);

    Assert.True(called);
  }

  [Fact]
  public void Implicit_Conversion_From_Error_Should_Create_Failure_Result()
  {
    Result result = new Error("Direkter Fehler");

    Assert.True(result.IsFailure);
    Assert.Equal("Direkter Fehler", result.Error.Message);
  }
}

public sealed class ResultOfTTests
{
  [Fact]
  public void Ok_Should_Create_Success_Result_With_Value()
  {
    var result = Result.Ok(42);

    Assert.True(result.IsSuccess);
    Assert.False(result.IsFailure);
    Assert.Equal(42, result.Value);
  }

  [Fact]
  public void Fail_With_Message_Should_Create_Failure_Result()
  {
    var result = Result.Fail<int>("Fehler");

    Assert.True(result.IsFailure);
    Assert.Equal("Fehler", result.Error.Message);
  }

  [Fact]
  public void Fail_With_Error_Should_Create_Failure_Result()
  {
    var error = new Error("Kaputt").WithCode("BROKEN");
    var result = Result.Fail<int>(error);

    Assert.True(result.IsFailure);
    Assert.Equal("Kaputt", result.Error.Message);
    Assert.Equal("BROKEN", result.Error.Code);
  }

  [Fact]
  public void Value_Should_Throw_When_Result_Is_Failure()
  {
    var result = Result.Fail<int>("Fehler");

    var ex = Assert.Throws<InvalidOperationException>(() => _ = result.Value);

    Assert.Contains("No value present", ex.Message);
  }

  [Fact]
  public void Error_Should_Throw_When_Result_Is_Success()
  {
    var result = Result.Ok(123);

    Assert.Throws<InvalidOperationException>(() => _ = result.Error);
  }

  [Fact]
  public void Map_Should_Transform_Value_When_Successful()
  {
    var result = Result.Ok(21);

    var mapped = result.Map(x => x * 2);

    Assert.True(mapped.IsSuccess);
    Assert.Equal(42, mapped.Value);
  }

  [Fact]
  public void Map_Should_Propagate_Error_When_Failed()
  {
    var result = Result.Fail<int>("Fehler");

    var mapped = result.Map(x => x * 2);

    Assert.True(mapped.IsFailure);
    Assert.Equal("Fehler", mapped.Error.Message);
  }

  [Fact]
  public void Bind_Should_Chain_Successful_Results()
  {
    var result = Result.Ok(10);

    var bound = result.Bind(x => Result.Ok(x + 5));

    Assert.True(bound.IsSuccess);
    Assert.Equal(15, bound.Value);
  }

  [Fact]
  public void Bind_Should_Propagate_Failure()
  {
    var result = Result.Fail<int>("Fehler");

    var bound = result.Bind(x => Result.Ok(x + 5));

    Assert.True(bound.IsFailure);
    Assert.Equal("Fehler", bound.Error.Message);
  }

  [Fact]
  public void Match_Should_Return_Success_Value_When_Result_Is_Ok()
  {
    var result = Result.Ok(7);

    var text = result.Match(
        onSuccess: x => $"Wert: {x}",
        onFailure: err => err.Message
    );

    Assert.Equal("Wert: 7", text);
  }

  [Fact]
  public void Match_Should_Return_Failure_Value_When_Result_Is_Failed()
  {
    var result = Result.Fail<int>("Fehler");

    var text = result.Match(
        onSuccess: x => $"Wert: {x}",
        onFailure: err => err.Message
    );

    Assert.Equal("Fehler", text);
  }

  [Fact]
  public void GetValueOrDefault_Should_Return_Value_When_Successful()
  {
    var result = Result.Ok(99);

    var value = result.GetValueOrDefault(-1);

    Assert.Equal(99, value);
  }

  [Fact]
  public void GetValueOrDefault_Should_Return_Default_When_Failed()
  {
    var result = Result.Fail<int>("Fehler");

    var value = result.GetValueOrDefault(-1);

    Assert.Equal(-1, value);
  }

  [Fact]
  public void GetValueOrElse_Should_Return_Fallback_When_Failed()
  {
    var result = Result.Fail<int>("Fehler");

    var value = result.GetValueOrElse(err => err.Message.Length);

    Assert.Equal(6, value);
  }

  [Fact]
  public void ToResult_Should_Return_NonGeneric_Success_Result()
  {
    var result = Result.Ok(42);

    var nonGeneric = result.ToResult();

    Assert.True(nonGeneric.IsSuccess);
    Assert.False(nonGeneric.IsFailure);
  }

  [Fact]
  public void ToResult_Should_Return_NonGeneric_Failure_Result()
  {
    var result = Result.Fail<int>("Fehler");

    var nonGeneric = result.ToResult();

    Assert.True(nonGeneric.IsFailure);
    Assert.Equal("Fehler", nonGeneric.Error.Message);
  }

  [Fact]
  public void Implicit_Conversion_From_Value_Should_Create_Success_Result()
  {
    Result<int> result = 42;

    Assert.True(result.IsSuccess);
    Assert.Equal(42, result.Value);
  }

  [Fact]
  public void Implicit_Conversion_From_Error_Should_Create_Failure_Result()
  {
    Result<int> result = new Error("Direkter Fehler");

    Assert.True(result.IsFailure);
    Assert.Equal("Direkter Fehler", result.Error.Message);
  }
}