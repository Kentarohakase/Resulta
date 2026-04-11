using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Resulta;

using Xunit;

namespace Resulta.Tests;

public sealed class ResultExtensionsTests
{
  // ── Try (sync, value) ────────────────────────────────────────────────────

  [Fact]
  public void Try_Should_Return_Ok_When_No_Exception_Is_Thrown()
  {
    var result = ResultExtensions.Try(() => int.Parse("42"));

    Assert.True(result.IsSuccess);
    Assert.Equal(42, result.Value);
  }

  [Fact]
  public void Try_Should_Return_Fail_When_Exception_Is_Thrown()
  {
    var result = ResultExtensions.Try<int>(() => throw new InvalidOperationException("Kaputt"));

    Assert.True(result.IsFailure);
    Assert.Equal("UNEXPECTED_ERROR", result.Error.Code);
  }

  [Fact]
  public void Try_Should_Use_Custom_ErrorMapper_When_Exception_Is_Thrown()
  {
    var result = ResultExtensions.Try(
        () => int.Parse("keine zahl"),
        ex => new Error("Parsing fehlgeschlagen").WithCode("PARSE_ERROR")
    );

    Assert.True(result.IsFailure);
    Assert.Equal("PARSE_ERROR", result.Error.Code);
    Assert.Equal("Parsing fehlgeschlagen", result.Error.Message);
  }

  // ── Try (sync, void) ─────────────────────────────────────────────────────

  [Fact]
  public void Try_Void_Should_Return_Ok_When_No_Exception_Is_Thrown()
  {
    var called = false;
    var result = ResultExtensions.Try(() => { called = true; });

    Assert.True(result.IsSuccess);
    Assert.True(called);
  }

  [Fact]
  public void Try_Void_Should_Return_Fail_When_Exception_Is_Thrown()
  {
    var result = ResultExtensions.Try(
        () => throw new InvalidOperationException("Kaputt"),
        ex => new Error(ex.Message).WithCode("ACTION_ERROR")
    );

    Assert.True(result.IsFailure);
    Assert.Equal("ACTION_ERROR", result.Error.Code);
    Assert.Equal("Kaputt", result.Error.Message);
  }

  // ── TryAsync ─────────────────────────────────────────────────────────────

  [Fact]
  public async Task TryAsync_Should_Return_Ok_When_No_Exception_Is_Thrown()
  {
    var result = await ResultExtensions.TryAsync(async () =>
    {
      await Task.Delay(1);
      return 99;
    });

    Assert.True(result.IsSuccess);
    Assert.Equal(99, result.Value);
  }

  [Fact]
  public async Task TryAsync_Should_Return_Fail_When_Exception_Is_Thrown()
  {
    var result = await ResultExtensions.TryAsync<int>(async () =>
    {
      await Task.Delay(1);
      throw new InvalidOperationException("Async Kaputt");
    });

    Assert.True(result.IsFailure);
    Assert.Equal("UNEXPECTED_ERROR", result.Error.Code);
  }

  [Fact]
  public async Task TryAsync_Should_Use_Custom_ErrorMapper_When_Exception_Is_Thrown()
  {
    var result = await ResultExtensions.TryAsync<int>(
        async () =>
        {
          await Task.Delay(1);
          throw new TimeoutException("Timeout");
        },
        ex => new Error("Zeitüberschreitung").WithCode("TIMEOUT")
    );

    Assert.True(result.IsFailure);
    Assert.Equal("TIMEOUT", result.Error.Code);
    Assert.Equal("Zeitüberschreitung", result.Error.Message);
  }

  // ── Combine (non-generic) ────────────────────────────────────────────────

  [Fact]
  public void Combine_NonGeneric_Should_Return_Ok_When_All_Results_Succeed()
  {
    var result = ResultExtensions.Combine(
        Result.Ok(),
        Result.Ok(),
        Result.Ok()
    );

    Assert.True(result.IsSuccess);
  }

  [Fact]
  public void Combine_NonGeneric_Should_Return_Fail_When_Any_Result_Fails()
  {
    var result = ResultExtensions.Combine(
        Result.Ok(),
        Result.Fail("Fehler A"),
        Result.Ok()
    );

    Assert.True(result.IsFailure);
    Assert.Equal("Fehler A", result.Error.Message);
  }

  [Fact]
  public void Combine_NonGeneric_Should_Chain_Multiple_Errors_As_Causes()
  {
    var result = ResultExtensions.Combine(
        Result.Fail("Fehler A"),
        Result.Ok(),
        Result.Fail("Fehler B")
    );

    Assert.True(result.IsFailure);
    Assert.Equal("Fehler A", result.Error.Message);
    Assert.NotNull(result.Error.CausedBy);
    Assert.Equal("Fehler B", result.Error.CausedBy!.Message);
  }

  // ── Combine (generic, params) ────────────────────────────────────────────

  [Fact]
  public void Combine_Generic_Should_Return_Ok_With_All_Values_When_All_Succeed()
  {
    var result = ResultExtensions.Combine(
        Result.Ok(1),
        Result.Ok(2),
        Result.Ok(3)
    );

    Assert.True(result.IsSuccess);
    Assert.Equal(new[] { 1, 2, 3 }, result.Value);
  }

  [Fact]
  public void Combine_Generic_Should_Return_Fail_When_Any_Result_Fails()
  {
    var result = ResultExtensions.Combine(
        Result.Ok(1),
        Result.Fail<int>("Fehler A"),
        Result.Ok(3)
    );

    Assert.True(result.IsFailure);
    Assert.Equal("Fehler A", result.Error.Message);
  }

  [Fact]
  public void Combine_Generic_Should_Chain_Multiple_Errors_As_Causes()
  {
    var result = ResultExtensions.Combine(
        Result.Fail<int>("Fehler A"),
        Result.Ok(2),
        Result.Fail<int>("Fehler B")
    );

    Assert.True(result.IsFailure);
    Assert.Equal("Fehler A", result.Error.Message);
    Assert.NotNull(result.Error.CausedBy);
    Assert.Equal("Fehler B", result.Error.CausedBy!.Message);
  }

  // ── Combine (generic, IEnumerable) ───────────────────────────────────────

  [Fact]
  public void Combine_Generic_IEnumerable_Should_Return_Ok_With_All_Values()
  {
    var results = new List<Result<string>>
        {
            Result.Ok("a"),
            Result.Ok("b"),
            Result.Ok("c")
        };

    var result = ResultExtensions.Combine(results);

    Assert.True(result.IsSuccess);
    Assert.Equal(new[] { "a", "b", "c" }, result.Value);
  }

  // ── Ensure ───────────────────────────────────────────────────────────────

  [Fact]
  public void Ensure_Should_Return_Ok_When_Predicate_Is_Met()
  {
    var result = Result.Ok(10)
        .Ensure(x => x > 5, "Muss größer als 5 sein");

    Assert.True(result.IsSuccess);
    Assert.Equal(10, result.Value);
  }

  [Fact]
  public void Ensure_Should_Return_Fail_When_Predicate_Is_Not_Met()
  {
    var result = Result.Ok(3)
        .Ensure(x => x > 5, "Muss größer als 5 sein");

    Assert.True(result.IsFailure);
    Assert.Equal("Muss größer als 5 sein", result.Error.Message);
    Assert.Equal("ENSURE_FAILED", result.Error.Code);
  }

  [Fact]
  public void Ensure_Should_Propagate_Existing_Failure_Without_Evaluating_Predicate()
  {
    var predicateCalled = false;

    var result = Result.Fail<int>("Ursprünglicher Fehler")
        .Ensure(x => { predicateCalled = true; return x > 5; }, "Muss größer als 5 sein");

    Assert.True(result.IsFailure);
    Assert.Equal("Ursprünglicher Fehler", result.Error.Message);
    Assert.False(predicateCalled);
  }

  [Fact]
  public void Ensure_With_Error_Object_Should_Return_That_Error_When_Predicate_Fails()
  {
    var customError = Error.Validation("alter", "Muss mindestens 18 sein");

    var result = Result.Ok(16)
        .Ensure(x => x >= 18, customError);

    Assert.True(result.IsFailure);
    Assert.Equal("VALIDATION_ERROR", result.Error.Code);
  }

  // ── MapAsync ─────────────────────────────────────────────────────────────

  [Fact]
  public async Task MapAsync_Should_Transform_Value_When_Successful()
  {
    var result = await Result.Ok(5)
        .MapAsync(async x =>
        {
          await Task.Delay(1);
          return x * 2;
        });

    Assert.True(result.IsSuccess);
    Assert.Equal(10, result.Value);
  }

  [Fact]
  public async Task MapAsync_Should_Propagate_Failure_Without_Calling_Mapper()
  {
    var mapperCalled = false;

    var result = await Result.Fail<int>("Fehler")
        .MapAsync(async x =>
        {
          mapperCalled = true;
          await Task.Delay(1);
          return x * 2;
        });

    Assert.True(result.IsFailure);
    Assert.Equal("Fehler", result.Error.Message);
    Assert.False(mapperCalled);
  }

  // ── BindAsync ────────────────────────────────────────────────────────────

  [Fact]
  public async Task BindAsync_Should_Chain_Successful_Async_Results()
  {
    var result = await Result.Ok(10)
        .BindAsync(async x =>
        {
          await Task.Delay(1);
          return Result.Ok(x + 5);
        });

    Assert.True(result.IsSuccess);
    Assert.Equal(15, result.Value);
  }

  [Fact]
  public async Task BindAsync_Should_Propagate_Failure_Without_Calling_Binder()
  {
    var binderCalled = false;

    var result = await Result.Fail<int>("Fehler")
        .BindAsync(async x =>
        {
          binderCalled = true;
          await Task.Delay(1);
          return Result.Ok(x + 5);
        });

    Assert.True(result.IsFailure);
    Assert.Equal("Fehler", result.Error.Message);
    Assert.False(binderCalled);
  }

  [Fact]
  public async Task BindAsync_Should_Return_Failure_When_Binder_Returns_Fail()
  {
    var result = await Result.Ok(10)
        .BindAsync(async x =>
        {
          await Task.Delay(1);
          return Result.Fail<int>("Binder Fehler");
        });

    Assert.True(result.IsFailure);
    Assert.Equal("Binder Fehler", result.Error.Message);
  }

  // ── Task<Result> Passthrough (Map, Bind, Match) ──────────────────────────

  [Fact]
  public async Task Task_Map_Should_Transform_Value_On_Successful_Task_Result()
  {
    var taskResult = Task.FromResult(Result.Ok(7));

    var result = await taskResult.Map(x => x * 3);

    Assert.True(result.IsSuccess);
    Assert.Equal(21, result.Value);
  }

  [Fact]
  public async Task Task_Bind_Should_Chain_On_Successful_Task_Result()
  {
    var taskResult = Task.FromResult(Result.Ok(4));

    var result = await taskResult.Bind(x => Result.Ok(x + 1));

    Assert.True(result.IsSuccess);
    Assert.Equal(5, result.Value);
  }

  [Fact]
  public async Task Task_Match_Should_Return_Success_Value_On_Ok()
  {
    var taskResult = Task.FromResult(Result.Ok(8));

    var text = await taskResult.Match(
        onSuccess: x => $"Wert: {x}",
        onFailure: err => err.Message
    );

    Assert.Equal("Wert: 8", text);
  }

  [Fact]
  public async Task Task_Match_Should_Return_Failure_Value_On_Fail()
  {
    var taskResult = Task.FromResult(Result.Fail<int>("Async Fehler"));

    var text = await taskResult.Match(
        onSuccess: x => $"Wert: {x}",
        onFailure: err => err.Message
    );

    Assert.Equal("Async Fehler", text);
  }
}