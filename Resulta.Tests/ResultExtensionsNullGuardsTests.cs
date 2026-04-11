using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Resulta;

using Xunit;

namespace Resulta.Tests;

public sealed class ResultExtensionsNullGuardsTests
{
  [Fact]
  public async Task MapAsync_Should_Throw_When_Result_Is_Null()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        ResultExtensions.MapAsync<int, string>(null!, _ => Task.FromResult("x")));
  }

  [Fact]
  public async Task MapAsync_Should_Throw_When_Mapper_Is_Null()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        Result.Ok(1).MapAsync<int, int>(null!));
  }

  [Fact]
  public async Task BindAsync_Should_Throw_When_Binder_Is_Null()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        Result.Ok(1).BindAsync((Func<int, Task<Result<int>>>)null!));
  }

  [Fact]
  public async Task Task_Map_Should_Throw_When_Task_Is_Null()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        ResultExtensions.Map(null!, (Func<int, int>)(_ => _)));
  }

  [Fact]
  public async Task Task_Bind_Should_Throw_When_Binder_Is_Null()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        Task.FromResult(Result.Ok(1)).Bind((Func<int, Result<int>>)null!));
  }

  [Fact]
  public async Task Task_Match_Should_Throw_When_OnSuccess_Is_Null()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        Task.FromResult(Result.Ok(1)).Match((Func<int, int>)null!, _ => 0));
  }

  [Fact]
  public void Combine_Params_Should_Throw_When_Array_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() => ResultExtensions.Combine((Result[])null!));
  }

  [Fact]
  public void Combine_Params_Should_Throw_When_Element_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() => ResultExtensions.Combine(Result.Ok(), null!));
  }

  [Fact]
  public void Combine_Generic_Params_Should_Throw_When_Array_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() => ResultExtensions.Combine((Result<int>[])null!));
  }

  [Fact]
  public void Combine_Generic_Enumerable_Should_Throw_When_Null()
  {
    Assert.Throws<ArgumentNullException>(() => ResultExtensions.Combine((IEnumerable<Result<int>>)null!));
  }

  [Fact]
  public void Combine_Generic_Enumerable_Should_Throw_When_Element_Is_Null()
  {
    var list = new List<Result<int>> { Result.Ok(1), null! };
    Assert.Throws<ArgumentNullException>(() => ResultExtensions.Combine(list));
  }

  [Fact]
  public async Task CombineAsync_Should_Throw_When_Tasks_Is_Null()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        ResultExtensions.CombineAsync((Task<Result<int>>[])null!));
  }

  [Fact]
  public async Task CombineAsync_Should_Throw_When_Task_Element_Is_Null()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        ResultExtensions.CombineAsync(Task.FromResult(Result.Ok(1)), null!));
  }

  [Fact]
  public async Task CombineAsync_IEnumerable_Should_Throw_When_Null()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        ResultExtensions.CombineAsync((IEnumerable<Task<Result<int>>>)null!));
  }

  [Fact]
  public void Ensure_Should_Throw_ArgumentNullException_When_Predicate_Is_Null()
  {
    var result = Result.Ok(42);

    Assert.Throws<ArgumentNullException>(() =>
        result.Ensure((Func<int, bool>)null!, "Fehler"));
  }

  [Fact]
  public void Ensure_Should_Throw_When_Predicate_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        Result.Ok(1).Ensure((Func<int, bool>)null!, "err"));
  }

  [Fact]
  public void Ensure_Should_Throw_When_ErrorMessage_Is_Null_Or_Whitespace()
  {
    Assert.Throws<ArgumentNullException>(() => Result.Ok(1).Ensure(_ => true, (string)null!));
    Assert.Throws<ArgumentException>(() => Result.Ok(1).Ensure(_ => true, "   "));
  }

  [Fact]
  public void Ensure_With_Error_Should_Throw_When_Error_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        Result.Ok(1).Ensure(_ => true, (Error)null!));
  }

  [Fact]
  public void Try_Should_Throw_When_Func_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() => ResultExtensions.Try((Func<int>)null!));
  }

  [Fact]
  public void Try_Action_Should_Throw_When_Action_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() => ResultExtensions.Try((Action)null!));
  }

  [Fact]
  public async Task TryAsync_Should_Throw_When_Func_Is_Null()
  {
    await Assert.ThrowsAsync<ArgumentNullException>(() =>
        ResultExtensions.TryAsync((Func<Task<int>>)null!));
  }
}
