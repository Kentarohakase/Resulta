using System.Collections.Generic;
using System.Threading.Tasks;

using Resulta;
using Resulta.Extensions;

using Xunit;

namespace Resulta.Tests;

public sealed class AsyncPipelineNewFeaturesTests
{
  // ── Validate (string) ─────────────────────────────────────────────────────

  [Fact]
  public async Task Validate_Should_Continue_When_Predicate_Is_Met()
  {
    var result = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Ok(10)))
        .Validate(x => x > 5, "Muss größer als 5 sein")
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal(10, result.Value);
  }

  [Fact]
  public async Task Validate_Should_Fail_When_Predicate_Is_Not_Met()
  {
    var result = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Ok(3)))
        .Validate(x => x > 5, "Muss größer als 5 sein")
        .Build();

    Assert.True(result.IsFailure);
    Assert.Equal("Muss größer als 5 sein", result.Error.Message);
    Assert.Equal("ENSURE_FAILED", result.Error.Code);
  }

  [Fact]
  public async Task Validate_Should_Skip_When_Already_Failed()
  {
    var predicateCalled = false;

    var result = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Fail<int>("Vorheriger Fehler")))
        .Validate(x => { predicateCalled = true; return x > 5; }, "Muss größer als 5 sein")
        .Build();

    Assert.True(result.IsFailure);
    Assert.Equal("Vorheriger Fehler", result.Error.Message);
    Assert.False(predicateCalled);
  }

  // ── Validate (Error object) ───────────────────────────────────────────────

  [Fact]
  public async Task Validate_With_Error_Object_Should_Return_That_Error_When_Predicate_Fails()
  {
    var customError = Error.Validation("alter", "Muss mindestens 18 sein");

    var result = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Ok(16)))
        .Validate(x => x >= 18, customError)
        .Build();

    Assert.True(result.IsFailure);
    Assert.Equal("VALIDATION_ERROR", result.Error.Code);
  }

  // ── Tap ───────────────────────────────────────────────────────────────────

  [Fact]
  public async Task Tap_Should_Execute_Side_Effect_On_Success()
  {
    var sideEffectValue = 0;

    var result = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Ok(7)))
        .Tap(x => sideEffectValue = x)
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal(7, result.Value);
    Assert.Equal(7, sideEffectValue);
  }

  [Fact]
  public async Task Tap_Should_Not_Execute_Side_Effect_On_Failure()
  {
    var sideEffectCalled = false;

    var result = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Fail<int>("Fehler")))
        .Tap(_ => sideEffectCalled = true)
        .Build();

    Assert.True(result.IsFailure);
    Assert.False(sideEffectCalled);
  }

  [Fact]
  public async Task Tap_Should_Not_Change_The_Result_Value()
  {
    var result = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Ok(99)))
        .Tap(_ => { /* side effect */ })
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal(99, result.Value);
  }

  // ── TapAsync ──────────────────────────────────────────────────────────────

  [Fact]
  public async Task TapAsync_Should_Execute_Async_Side_Effect_On_Success()
  {
    var log = new List<string>();

    var result = await AsyncPipeline<string>
        .Start(() => Task.FromResult(Result.Ok("kentaro")))
        .TapAsync(async s =>
        {
          await Task.Delay(1);
          log.Add($"Verarbeitet: {s}");
        })
        .Build();

    Assert.True(result.IsSuccess);
    Assert.Equal("kentaro", result.Value);
    Assert.Single(log);
    Assert.Equal("Verarbeitet: kentaro", log[0]);
  }

  [Fact]
  public async Task TapAsync_Should_Not_Execute_Side_Effect_On_Failure()
  {
    var sideEffectCalled = false;

    var result = await AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Fail<int>("Fehler")))
        .TapAsync(async _ =>
        {
          await Task.Delay(1);
          sideEffectCalled = true;
        })
        .Build();

    Assert.True(result.IsFailure);
    Assert.False(sideEffectCalled);
  }

  // ── Full pipeline with new features ──────────────────────────────────────

  [Fact]
  public async Task Full_Pipeline_With_Validate_Tap_TapAsync_Should_Work()
  {
    var log = new List<string>();

    var message = await AsyncPipeline<string>
        .Start(() => Task.FromResult(Result.Ok("  kentaro  ")))
        .Then(s => Result.Ok(s.Trim()))
        .Validate(s => s.Length >= 2, "Name zu kurz")
        .Tap(s => log.Add($"Sync: {s}"))
        .TapAsync(async s =>
        {
          await Task.Delay(1);
          log.Add($"Async: {s}");
        })
        .ThenAsync(s => Task.FromResult(Result.Ok(s.ToUpperInvariant())))
        .Finally(
            onSuccess: s => $"TOKEN-{s}",
            onFailure: err => $"Fehler: {err.Message}"
        );

    Assert.Equal("TOKEN-KENTARO", message);
    Assert.Equal(2, log.Count);
    Assert.Equal("Sync: kentaro", log[0]);
    Assert.Equal("Async: kentaro", log[1]);
  }
}