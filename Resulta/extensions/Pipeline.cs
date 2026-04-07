namespace Resulta.Extensions
{
  /// <summary>
  /// A synchronous Railway-Oriented Pipeline that chains processing steps elegantly.
  /// Each step is only executed if the previous one succeeded.
  /// Use <see cref="Start(T)"/> or <see cref="Start(Result{T})"/> as the entry point.
  /// </summary>
  /// <typeparam name="T">The type of the current pipeline value.</typeparam>
  public class Pipeline<T>
  {
    private readonly Result<T> _current;

    private Pipeline(Result<T> result) => _current = result;

    // ── Entry Points ─────────────────────────────────────────────────────

    /// <summary>Starts a new pipeline with a plain success value.</summary>
    /// <param name="value">The initial value to wrap in a successful result.</param>
    public static Pipeline<T> Start(T value)
        => new Pipeline<T>(Result<T>.Ok(value));

    /// <summary>Starts a new pipeline from an existing <see cref="Result{T}"/>.</summary>
    /// <param name="result">The result to start the pipeline with.</param>
    public static Pipeline<T> Start(Result<T> result)
        => new Pipeline<T>(result);

    // ── Steps ────────────────────────────────────────────────────────────

    /// <summary>
    /// Chains a step that returns a <see cref="Result{TOut}"/>.
    /// Skipped without invoking <paramref name="step"/> if the pipeline has already failed.
    /// </summary>
    /// <typeparam name="TOut">The type of the next step's value.</typeparam>
    /// <param name="step">The function to apply to the current value.</param>
    public Pipeline<TOut> Then<TOut>(Func<T, Result<TOut>> step)
        => new Pipeline<TOut>(_current.Bind(step));

    /// <summary>
    /// Chains a step that returns a plain value of type <typeparamref name="TOut"/>.
    /// Skipped without invoking <paramref name="step"/> if the pipeline has already failed.
    /// </summary>
    /// <typeparam name="TOut">The type of the next step's value.</typeparam>
    /// <param name="step">The function to apply to the current value.</param>
    public Pipeline<TOut> Then<TOut>(Func<T, TOut> step)
        => new Pipeline<TOut>(_current.Map(step));

    /// <summary>
    /// Validates the current value against <paramref name="predicate"/>.
    /// Fails the pipeline with <paramref name="errorMessage"/> if the predicate is not met.
    /// Skipped if the pipeline has already failed.
    /// </summary>
    /// <param name="predicate">The condition the value must satisfy.</param>
    /// <param name="errorMessage">The error message to use when the predicate fails.</param>
    public Pipeline<T> Validate(Func<T, bool> predicate, string errorMessage)
        => new Pipeline<T>(_current.Ensure(predicate, errorMessage));

    /// <summary>
    /// Validates the current value against <paramref name="predicate"/>.
    /// Fails the pipeline with <paramref name="error"/> if the predicate is not met.
    /// Skipped if the pipeline has already failed.
    /// </summary>
    /// <param name="predicate">The condition the value must satisfy.</param>
    /// <param name="error">The error to use when the predicate fails.</param>
    public Pipeline<T> Validate(Func<T, bool> predicate, Error error)
        => new Pipeline<T>(_current.Ensure(predicate, error));

    /// <summary>
    /// Executes a side effect <paramref name="action"/> if the pipeline is still successful.
    /// The value passes through unchanged.
    /// Skipped if the pipeline has already failed.
    /// </summary>
    /// <param name="action">The side effect to execute, receiving the current value.</param>
    public Pipeline<T> Tap(Action<T> action)
        => new Pipeline<T>(_current.OnSuccess(action));

    // ── Termination ──────────────────────────────────────────────────────

    /// <summary>
    /// Terminates the pipeline and handles both the success and failure case,
    /// returning a value of type <typeparamref name="TOut"/>.
    /// </summary>
    /// <typeparam name="TOut">The return type.</typeparam>
    /// <param name="onSuccess">Invoked with the value when the pipeline succeeded.</param>
    /// <param name="onFailure">Invoked with the error when the pipeline failed.</param>
    public TOut Finally<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
        => _current.Match(onSuccess, onFailure);

    /// <summary>
    /// Terminates the pipeline with void actions for both the success and failure case.
    /// </summary>
    /// <param name="onSuccess">Invoked with the value when the pipeline succeeded.</param>
    /// <param name="onFailure">Invoked with the error when the pipeline failed.</param>
    public void Finally(Action<T> onSuccess, Action<Error> onFailure)
        => _current.Match(onSuccess, onFailure);

    /// <summary>Returns the underlying <see cref="Result{T}"/> without handling it.</summary>
    public Result<T> Build() => _current;

    // ── Async Bridge ─────────────────────────────────────────────────────

    /// <summary>
    /// Transitions to an <see cref="AsyncPipeline{T}"/> by chaining an async step,
    /// keeping the same type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="step">The async function to apply to the current value.</param>
    public AsyncPipeline<T> ThenAsync(Func<T, Task<Result<T>>> step)
        => new AsyncPipeline<T>(Task.FromResult(_current)).ThenAsync(step);

    /// <summary>
    /// Transitions to an <see cref="AsyncPipeline{TOut}"/> by chaining an async step
    /// with a type change from <typeparamref name="T"/> to <typeparamref name="TOut"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the next step's value.</typeparam>
    /// <param name="step">The async function to apply to the current value.</param>
    public AsyncPipeline<TOut> ThenAsync<TOut>(Func<T, Task<Result<TOut>>> step)
        => new AsyncPipeline<TOut>(Task.FromResult(_current).Bind(v => step(v)));
  }

  /// <summary>
  /// An async-capable Railway-Oriented Pipeline that chains processing steps elegantly.
  /// Each step is only executed if the previous one succeeded.
  /// Use <see cref="Start"/> as the entry point, or transition from <see cref="Pipeline{T}"/> via <c>ThenAsync</c>.
  /// </summary>
  /// <typeparam name="T">The type of the current pipeline value.</typeparam>
  public class AsyncPipeline<T>
  {
    private readonly Task<Result<T>> _task;

    internal AsyncPipeline(Task<Result<T>> task) => _task = task;

    /// <summary>
    /// Starts a new async pipeline from a factory function that returns a <see cref="Task{Result}"/>.
    /// </summary>
    /// <param name="factory">A factory function that produces the initial async result.</param>
    public static AsyncPipeline<T> Start(Func<Task<Result<T>>> factory)
        => new AsyncPipeline<T>(factory());

    // ── Steps ────────────────────────────────────────────────────────────

    /// <summary>
    /// Chains an async step keeping the same type <typeparamref name="T"/>.
    /// Skipped without invoking <paramref name="step"/> if the pipeline has already failed.
    /// </summary>
    /// <param name="step">The async function to apply to the current value.</param>
    public AsyncPipeline<T> ThenAsync(Func<T, Task<Result<T>>> step)
        => new AsyncPipeline<T>(_task.Bind(v => step(v)));

    /// <summary>
    /// Chains an async step with a type change to <typeparamref name="TOut"/>.
    /// Skipped without invoking <paramref name="step"/> if the pipeline has already failed.
    /// </summary>
    /// <typeparam name="TOut">The type of the next step's value.</typeparam>
    /// <param name="step">The async function to apply to the current value.</param>
    public AsyncPipeline<TOut> ThenAsync<TOut>(Func<T, Task<Result<TOut>>> step)
        => new AsyncPipeline<TOut>(_task.Bind(step));

    /// <summary>
    /// Chains a synchronous step. Skipped without invoking <paramref name="step"/> if the pipeline has already failed.
    /// </summary>
    /// <typeparam name="TOut">The type of the next step's value.</typeparam>
    /// <param name="step">The function to apply to the current value.</param>
    public AsyncPipeline<TOut> Then<TOut>(Func<T, Result<TOut>> step)
        => new AsyncPipeline<TOut>(_task.Bind(v => Task.FromResult(step(v))));

    /// <summary>
    /// Validates the current value against <paramref name="predicate"/>.
    /// Fails the pipeline with <paramref name="errorMessage"/> if the predicate is not met.
    /// Skipped if the pipeline has already failed.
    /// </summary>
    /// <param name="predicate">The condition the value must satisfy.</param>
    /// <param name="errorMessage">The error message to use when the predicate fails.</param>
    public AsyncPipeline<T> Validate(Func<T, bool> predicate, string errorMessage)
    {
      var next = _task.Bind(v =>
          Task.FromResult(
              predicate(v)
                  ? Result<T>.Ok(v)
                  : Result<T>.Fail(new Error(errorMessage, code: "ENSURE_FAILED"))
          ));
      return new AsyncPipeline<T>(next);
    }

    /// <summary>
    /// Validates the current value against <paramref name="predicate"/>.
    /// Fails the pipeline with <paramref name="error"/> if the predicate is not met.
    /// Skipped if the pipeline has already failed.
    /// </summary>
    /// <param name="predicate">The condition the value must satisfy.</param>
    /// <param name="error">The error to use when the predicate fails.</param>
    public AsyncPipeline<T> Validate(Func<T, bool> predicate, Error error)
    {
      var next = _task.Bind(v =>
          Task.FromResult(
              predicate(v)
                  ? Result<T>.Ok(v)
                  : Result<T>.Fail(error)
          ));
      return new AsyncPipeline<T>(next);
    }

    /// <summary>
    /// Executes a synchronous side effect <paramref name="action"/> if the pipeline is still successful.
    /// The value passes through unchanged.
    /// Skipped if the pipeline has already failed.
    /// </summary>
    /// <param name="action">The side effect to execute, receiving the current value.</param>
    public AsyncPipeline<T> Tap(Action<T> action)
    {
      var next = _task.Bind(v =>
      {
        action(v);
        return Task.FromResult(Result<T>.Ok(v));
      });
      return new AsyncPipeline<T>(next);
    }

    /// <summary>
    /// Executes an async side effect <paramref name="action"/> if the pipeline is still successful.
    /// The value passes through unchanged.
    /// Skipped if the pipeline has already failed.
    /// </summary>
    /// <param name="action">The async side effect to execute, receiving the current value.</param>
    public AsyncPipeline<T> TapAsync(Func<T, Task> action)
    {
      var next = _task.Bind(async v =>
      {
        await action(v);
        return Result<T>.Ok(v);
      });
      return new AsyncPipeline<T>(next);
    }

    // ── Termination ──────────────────────────────────────────────────────

    /// <summary>
    /// Terminates the async pipeline and handles both the success and failure case,
    /// returning a value of type <typeparamref name="TOut"/>.
    /// </summary>
    /// <typeparam name="TOut">The return type.</typeparam>
    /// <param name="onSuccess">Invoked with the value when the pipeline succeeded.</param>
    /// <param name="onFailure">Invoked with the error when the pipeline failed.</param>
    public async Task<TOut> Finally<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
        => (await _task).Match(onSuccess, onFailure);

    /// <summary>
    /// Terminates the async pipeline with void actions for both the success and failure case.
    /// </summary>
    /// <param name="onSuccess">Invoked with the value when the pipeline succeeded.</param>
    /// <param name="onFailure">Invoked with the error when the pipeline failed.</param>
    public async Task Finally(Action<T> onSuccess, Action<Error> onFailure)
        => (await _task).Match(onSuccess, onFailure);

    /// <summary>Returns the underlying <see cref="Task"/>-wrapped <see cref="Result{T}"/> without handling it.</summary>
    public Task<Result<T>> Build() => _task;
  }

  // ── Internal Helper ───────────────────────────────────────────────────────

  internal static class TaskResultExtensions
  {
    public static async Task<Result<TOut>> Bind<T, TOut>(
        this Task<Result<T>> task, Func<T, Task<Result<TOut>>> binder)
    {
      var result = await task;
      return result.IsFailure ? Result<TOut>.Fail(result.Error) : await binder(result.Value);
    }
  }
}