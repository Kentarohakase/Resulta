using System;
using System.Threading.Tasks;

using Resulta;
using Resulta.Extensions;

using Xunit;

namespace Resulta.Tests;

public sealed class PipelineNullGuardsTests
{
  [Fact]
  public void Pipeline_Start_Result_Should_Throw_When_Null()
  {
    Assert.Throws<ArgumentNullException>(() => Pipeline<int>.Start((Result<int>)null!));
  }

  [Fact]
  public void Pipeline_Then_Should_Throw_When_Step_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        Pipeline<int>.Start(1).Then((Func<int, int>)null!));
  }

  [Fact]
  public void Pipeline_Then_Result_Should_Throw_When_Step_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        Pipeline<int>.Start(1).Then((Func<int, Result<int>>)null!));
  }

  [Fact]
  public void Pipeline_Validate_Should_Throw_When_Predicate_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        Pipeline<int>.Start(1).Validate((Func<int, bool>)null!, "err"));
  }

  [Fact]
  public void Pipeline_Validate_Should_Throw_When_ErrorMessage_Is_Invalid()
  {
    Assert.Throws<ArgumentException>(() =>
        Pipeline<int>.Start(1).Validate(_ => true, "  "));
  }

  [Fact]
  public void Pipeline_Validate_Error_Should_Throw_When_Error_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        Pipeline<int>.Start(1).Validate(_ => true, (Error)null!));
  }

  [Fact]
  public void Pipeline_Tap_Should_Throw_When_Action_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        Pipeline<int>.Start(1).Tap((Action<int>)null!));
  }

  [Fact]
  public void Pipeline_Finally_Should_Throw_When_Delegate_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        Pipeline<int>.Start(1).Finally((Func<int, int>)null!, _ => 0));
    Assert.Throws<ArgumentNullException>(() =>
        Pipeline<int>.Start(1).Finally(_ => 0, (Func<Error, int>)null!));
  }

  [Fact]
  public void Pipeline_ThenAsync_Should_Throw_When_Step_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        Pipeline<int>.Start(1).ThenAsync((Func<int, Task<Result<int>>>)null!));
  }

  [Fact]
  public void AsyncPipeline_Start_Should_Throw_When_Factory_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        AsyncPipeline<int>.Start((Func<Task<Result<int>>>)null!));
  }

  [Fact]
  public void AsyncPipeline_Start_Should_Throw_When_Factory_Returns_Null_Task()
  {
    Assert.Throws<ArgumentNullException>(() =>
        AsyncPipeline<int>.Start(() => null!));
  }

  [Fact]
  public void AsyncPipeline_ThenAsync_Should_Throw_When_Step_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        AsyncPipeline<int>
            .Start(() => Task.FromResult(Result.Ok(1)))
            .ThenAsync((Func<int, Task<Result<int>>>)null!));
  }

  [Fact]
  public void AsyncPipeline_Then_Should_Throw_When_Step_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        AsyncPipeline<int>
            .Start(() => Task.FromResult(Result.Ok(1)))
            .Then((Func<int, Result<int>>)null!));
  }

  [Fact]
  public void AsyncPipeline_Validate_Should_Throw_When_Predicate_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        AsyncPipeline<int>
            .Start(() => Task.FromResult(Result.Ok(1)))
            .Validate((Func<int, bool>)null!, "e"));
  }

  [Fact]
  public void AsyncPipeline_Tap_Should_Throw_When_Action_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        AsyncPipeline<int>
            .Start(() => Task.FromResult(Result.Ok(1)))
            .Tap((Action<int>)null!));
  }

  [Fact]
  public void AsyncPipeline_TapAsync_Should_Throw_When_Action_Is_Null()
  {
    Assert.Throws<ArgumentNullException>(() =>
        AsyncPipeline<int>
            .Start(() => Task.FromResult(Result.Ok(1)))
            .TapAsync((Func<int, Task>)null!));
  }

  [Fact]
  public async Task AsyncPipeline_Finally_Should_Throw_When_OnSuccess_Is_Null()
  {
    var task = AsyncPipeline<int>
        .Start(() => Task.FromResult(Result.Ok(1)))
        .Finally((Func<int, int>)null!, _ => 0);

    await Assert.ThrowsAsync<ArgumentNullException>(() => task);
  }
}
