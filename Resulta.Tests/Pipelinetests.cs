using System;
using System.Threading.Tasks;

using Resulta;
using Resulta.Extensions;

using Xunit;

namespace Resulta.Tests;

public sealed class PipelineTests
{
  // ── Pipeline<T>.Start ────────────────────────────────────────────────────

  [Fact]
  public void Start_With_Value_Should_Create_Successful_Pipeline()
  {
    var result = Pipeline<int>
        .Start(42)
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal(42, result.Value);
  }

  [Fact]
  public void Start_With_Failed_Result_Should_Create_Failed_Pipeline()
  {
    var result = Pipeline<int>
        .Start(Result.Fail<int>("Fehler"))
        .Build();

    Assert.True(result.IsFailure);
    Assert.Equal("Fehler", result.Error.Message);
  }

  // ── Then (value) ─────────────────────────────────────────────────────────

  [Fact]
  public void Then_With_Value_Transform_Should_Map_Value()
  {
    var result = Pipeline<int>
        .Start(10)
        .Then(x => x * 2)
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal(20, result.Value);
  }

  [Fact]
  public void Then_With_Value_Transform_Should_Skip_When_Failed()
  {
    var called = false;

    var result = Pipeline<int>
        .Start(Result.Fail<int>("Fehler"))
        .Then(x => { called = true; return x * 2; })
        .Build();

    Assert.True(result.IsFailure);
    Assert.False(called);
  }

  // ── Then (Result) ─────────────────────────────────────────────────────────

  [Fact]
  public void Then_With_Result_Should_Chain_Successfully()
  {
    var result = Pipeline<int>
        .Start(5)
        .Then(x => Result.Ok(x + 3))
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal(8, result.Value);
  }

  [Fact]
  public void Then_With_Result_Should_Propagate_Failure()
  {
    var result = Pipeline<int>
        .Start(5)
        .Then(_ => Result.Fail<int>("Schritt fehlgeschlagen"))
        .Build();

    Assert.True(result.IsFailure);
    Assert.Equal("Schritt fehlgeschlagen", result.Error.Message);
  }

  [Fact]
  public void Then_Should_Skip_All_Subsequent_Steps_After_First_Failure()
  {
    var step2Called = false;
    var step3Called = false;

    var result = Pipeline<int>
        .Start(5)
        .Then(_ => Result.Fail<int>("Erster Fehler"))
        .Then(x => { step2Called = true; return Result.Ok(x + 1); })
        .Then(x => { step3Called = true; return x * 2; })
        .Build();

    Assert.True(result.IsFailure);
    Assert.False(step2Called);
    Assert.False(step3Called);
  }

  // ── Then (type change) ────────────────────────────────────────────────────

  [Fact]
  public void Then_Should_Support_Type_Change_Between_Steps()
  {
    var result = Pipeline<int>
        .Start(42)
        .Then(x => $"Wert: {x}")
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal("Wert: 42", result.Value);
  }

  // ── Validate ─────────────────────────────────────────────────────────────

  [Fact]
  public void Validate_Should_Continue_When_Predicate_Is_Met()
  {
    var result = Pipeline<int>
        .Start(10)
        .Validate(x => x > 5, "Muss größer als 5 sein")
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal(10, result.Value);
  }

  [Fact]
  public void Validate_Should_Fail_When_Predicate_Is_Not_Met()
  {
    var result = Pipeline<int>
        .Start(3)
        .Validate(x => x > 5, "Muss größer als 5 sein")
        .Build();

    Assert.True(result.IsFailure);
    Assert.Equal("Muss größer als 5 sein", result.Error.Message);
  }

  [Fact]
  public void Validate_With_Error_Object_Should_Use_That_Error_When_Predicate_Fails()
  {
    var customError = Error.Validation("alter", "Muss mindestens 18 sein");

    var result = Pipeline<int>
        .Start(16)
        .Validate(x => x >= 18, customError)
        .Build();

    Assert.True(result.IsFailure);
    Assert.Equal("VALIDATION_ERROR", result.Error.Code);
  }

  // ── Tap ──────────────────────────────────────────────────────────────────

  [Fact]
  public void Tap_Should_Execute_Side_Effect_On_Success()
  {
    var sideEffectValue = 0;

    var result = Pipeline<int>
        .Start(7)
        .Tap(x => sideEffectValue = x)
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal(7, sideEffectValue);
  }

  [Fact]
  public void Tap_Should_Not_Execute_Side_Effect_On_Failure()
  {
    var sideEffectCalled = false;

    var result = Pipeline<int>
        .Start(Result.Fail<int>("Fehler"))
        .Tap(_ => sideEffectCalled = true)
        .Build();

    Assert.True(result.IsFailure);
    Assert.False(sideEffectCalled);
  }

  [Fact]
  public void Tap_Should_Not_Change_The_Result_Value()
  {
    var result = Pipeline<int>
        .Start(99)
        .Tap(_ => { /* side effect */ })
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal(99, result.Value);
  }

  // ── Finally ───────────────────────────────────────────────────────────────

  [Fact]
  public void Finally_Should_Call_Success_Branch_On_Ok()
  {
    var message = Pipeline<int>
        .Start(5)
        .Finally(
            onSuccess: x => $"Wert: {x}",
            onFailure: err => $"Fehler: {err.Message}"
        );

    Assert.Equal("Wert: 5", message);
  }

  [Fact]
  public void Finally_Should_Call_Failure_Branch_On_Fail()
  {
    var message = Pipeline<int>
        .Start(Result.Fail<int>("Kaputt"))
        .Finally(
            onSuccess: x => $"Wert: {x}",
            onFailure: err => $"Fehler: {err.Message}"
        );

    Assert.Equal("Fehler: Kaputt", message);
  }

  // ── Full pipeline ─────────────────────────────────────────────────────────

  [Fact]
  public void Full_Pipeline_Should_Execute_All_Steps_In_Order()
  {
    var log = new System.Collections.Generic.List<string>();

    var token = Pipeline<string>
        .Start("kentaro")
        .Validate(s => !string.IsNullOrWhiteSpace(s), "Username darf nicht leer sein")
        .Then(s => s.Trim())
        .Then(s => Result.Ok(s.ToUpperInvariant()))
        .Tap(s => log.Add($"Verarbeitet: {s}"))
        .Then(s => $"TOKEN-{s}")
        .Finally(
            onSuccess: t => t,
            onFailure: err => $"Fehler: {err.Message}"
        );

    Assert.Equal("TOKEN-KENTARO", token);
    Assert.Single(log);
    Assert.Equal("Verarbeitet: KENTARO", log[0]);
  }
}

public sealed class AsyncPipelineTests
{
  // ── AsyncPipeline<T>.Start ────────────────────────────────────────────────

  [Fact]
  public async Task Start_Should_Create_Successful_Async_Pipeline()
  {
    var result = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Ok(42)))
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal(42, result.Value);
  }

  [Fact]
  public async Task Start_Should_Create_Failed_Async_Pipeline()
  {
    var result = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Fail<int>("Fehler")))
        .Build();

    Assert.True(result.IsFailure);
    Assert.Equal("Fehler", result.Error.Message);
  }

  // ── ThenAsync (same type) ─────────────────────────────────────────────────

  [Fact]
  public async Task ThenAsync_Should_Chain_Successful_Steps()
  {
    var result = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Ok(5)))
        .ThenAsync(async x =>
        {
          await Task.Delay(1);
          return Result.Ok(x * 2);
        })
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal(10, result.Value);
  }

  [Fact]
  public async Task ThenAsync_Should_Skip_Step_When_Already_Failed()
  {
    var stepCalled = false;

    var result = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Fail<int>("Fehler")))
        .ThenAsync(async x =>
        {
          stepCalled = true;
          await Task.Delay(1);
          return Result.Ok(x + 1);
        })
        .Build();

    Assert.True(result.IsFailure);
    Assert.False(stepCalled);
  }

  // ── ThenAsync (type change) ───────────────────────────────────────────────

  [Fact]
  public async Task ThenAsync_Should_Support_Type_Change_Between_Steps()
  {
    var result = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Ok(7)))
        .ThenAsync<string>(async x =>
        {
          await Task.Delay(1);
          return Result.Ok($"Wert: {x}");
        })
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal("Wert: 7", result.Value);
  }

  // ── Then (sync step in async pipeline) ───────────────────────────────────

  [Fact]
  public async Task Then_Sync_Should_Work_Inside_Async_Pipeline()
  {
    var result = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Ok(3)))
        .Then(x => Result.Ok(x + 10))
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal(13, result.Value);
  }

  // ── Finally ───────────────────────────────────────────────────────────────

  [Fact]
  public async Task Finally_Should_Call_Success_Branch_On_Ok()
  {
    var message = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Ok(5)))
        .Finally(
            onSuccess: x => $"Wert: {x}",
            onFailure: err => $"Fehler: {err.Message}"
        );

    Assert.Equal("Wert: 5", message);
  }

  [Fact]
  public async Task Finally_Should_Call_Failure_Branch_On_Fail()
  {
    var message = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Fail<int>("Async Fehler")))
        .Finally(
            onSuccess: x => $"Wert: {x}",
            onFailure: err => $"Fehler: {err.Message}"
        );

    Assert.Equal("Fehler: Async Fehler", message);
  }

  // ── Full async pipeline ───────────────────────────────────────────────────

  [Fact]
  public async Task Full_Async_Pipeline_Should_Execute_All_Steps_In_Order()
  {
    var log = new System.Collections.Generic.List<string>();

    var message = await AsyncPipeline<string>
        .Start(() => Task.FromResult(Result.Ok("kentaro")))
        .ThenAsync(async s =>
        {
          await Task.Delay(1);
          return Result.Ok(s.Trim());
        })
        .Then(s => Result.Ok(s.ToUpperInvariant()))
        .ThenAsync<string>(async s =>
        {
          await Task.Delay(1);
          log.Add($"Verarbeitet: {s}");
          return Result.Ok($"TOKEN-{s}");
        })
        .Finally(
            onSuccess: t => t,
            onFailure: err => $"Fehler: {err.Message}"
        );

    Assert.Equal("TOKEN-KENTARO", message);
    Assert.Single(log);
    Assert.Equal("Verarbeitet: KENTARO", log[0]);
  }

  // ── Pipeline<T> → AsyncPipeline<T> bridge ────────────────────────────────

  [Fact]
  public async Task Pipeline_ThenAsync_Should_Transition_To_Async_Pipeline()
  {
    var result = await Pipeline<int>
        .Start(10)
        .Then(x => x + 5)
        .ThenAsync(async x =>
        {
          await Task.Delay(1);
          return Result.Ok(x * 2);
        })
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal(30, result.Value);
  }

  [Fact]
  public async Task Pipeline_ThenAsync_With_Type_Change_Should_Work()
  {
    var result = await Pipeline<int>
        .Start(42)
        .ThenAsync<string>(async x =>
        {
          await Task.Delay(1);
          return Result.Ok($"Wert: {x}");
        })
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal("Wert: 42", result.Value);
  }
}