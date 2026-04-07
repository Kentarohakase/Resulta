using System.Collections.Generic;
using System.Threading.Tasks;

using Resulta;

using Xunit;

namespace Resulta.Tests;

public sealed class CombineAsyncTests
{
  // ── params overload ───────────────────────────────────────────────────────

  [Fact]
  public async Task CombineAsync_Should_Return_Ok_With_All_Values_When_All_Succeed()
  {
    var result = await ResultExtensions.CombineAsync(
        Task.FromResult(Result.Ok(1)),
        Task.FromResult(Result.Ok(2)),
        Task.FromResult(Result.Ok(3))
    );

    Assert.True(result.IsSuccess);
    Assert.Equal(new[] { 1, 2, 3 }, result.Value);
  }

  [Fact]
  public async Task CombineAsync_Should_Return_Fail_When_Any_Task_Fails()
  {
    var result = await ResultExtensions.CombineAsync(
        Task.FromResult(Result.Ok(1)),
        Task.FromResult(Result.Fail<int>("Fehler A")),
        Task.FromResult(Result.Ok(3))
    );

    Assert.True(result.IsFailure);
    Assert.Equal("Fehler A", result.Error.Message);
  }

  [Fact]
  public async Task CombineAsync_Should_Chain_Multiple_Errors_As_Causes()
  {
    var result = await ResultExtensions.CombineAsync(
        Task.FromResult(Result.Fail<int>("Fehler A")),
        Task.FromResult(Result.Ok(2)),
        Task.FromResult(Result.Fail<int>("Fehler B"))
    );

    Assert.True(result.IsFailure);
    Assert.Equal("Fehler A", result.Error.Message);
    Assert.NotNull(result.Error.CausedBy);
    Assert.Equal("Fehler B", result.Error.CausedBy!.Message);
  }

  [Fact]
  public async Task CombineAsync_Should_Run_Tasks_In_Parallel()
  {
    // Alle drei Tasks laufen gleichzeitig — Gesamtdauer ~50ms, nicht ~150ms
    var result = await ResultExtensions.CombineAsync(
        Task.Delay(50).ContinueWith(_ => Result.Ok(1)),
        Task.Delay(50).ContinueWith(_ => Result.Ok(2)),
        Task.Delay(50).ContinueWith(_ => Result.Ok(3))
    );

    Assert.True(result.IsSuccess);
    Assert.Equal(3, result.Value.Count);
  }

  // ── IEnumerable overload ──────────────────────────────────────────────────

  [Fact]
  public async Task CombineAsync_IEnumerable_Should_Return_Ok_With_All_Values()
  {
    var tasks = new List<Task<Result<string>>>
        {
            Task.FromResult(Result.Ok("a")),
            Task.FromResult(Result.Ok("b")),
            Task.FromResult(Result.Ok("c"))
        };

    var result = await ResultExtensions.CombineAsync(tasks);

    Assert.True(result.IsSuccess);
    Assert.Equal(new[] { "a", "b", "c" }, result.Value);
  }

  [Fact]
  public async Task CombineAsync_IEnumerable_Should_Return_Fail_When_Any_Fails()
  {
    var tasks = new List<Task<Result<string>>>
        {
            Task.FromResult(Result.Ok("a")),
            Task.FromResult(Result.Fail<string>("Fehler")),
            Task.FromResult(Result.Ok("c"))
        };

    var result = await ResultExtensions.CombineAsync(tasks);

    Assert.True(result.IsFailure);
    Assert.Equal("Fehler", result.Error.Message);
  }
}