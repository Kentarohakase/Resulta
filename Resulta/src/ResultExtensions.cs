namespace Resulta
{
  /// <summary>
  /// Extension methods for <see cref="Result"/> and <see cref="Result{T}"/> providing
  /// async support, exception wrapping, combining, and validation helpers.
  /// </summary>
  public static class ResultExtensions
  {
    // ── Async Map / Bind ─────────────────────────────────────────────────

    /// <summary>
    /// Asynchronously transforms the success value using <paramref name="mapper"/>.
    /// If the result has failed, the error is propagated without invoking the mapper.
    /// </summary>
    /// <typeparam name="T">The type of the current value.</typeparam>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <param name="result">The result to map over.</param>
    /// <param name="mapper">An async function to apply to the success value.</param>
    public static async Task<Result<TOut>> MapAsync<T, TOut>(
        this Result<T> result, Func<T, Task<TOut>> mapper)
    {
      if (result.IsFailure) return Result<TOut>.Fail(result.Error);
      var mapped = await mapper(result.Value);
      return Result<TOut>.Ok(mapped);
    }

    /// <summary>
    /// Asynchronously chains a function that returns a <see cref="Result{TOut}"/>.
    /// If the result has failed, the error is propagated without invoking <paramref name="binder"/>.
    /// </summary>
    /// <typeparam name="T">The type of the current value.</typeparam>
    /// <typeparam name="TOut">The type of the next result's value.</typeparam>
    /// <param name="result">The result to bind over.</param>
    /// <param name="binder">An async function to apply to the success value.</param>
    public static async Task<Result<TOut>> BindAsync<T, TOut>(
        this Result<T> result, Func<T, Task<Result<TOut>>> binder)
    {
      if (result.IsFailure) return Result<TOut>.Fail(result.Error);
      return await binder(result.Value);
    }

    /// <summary>
    /// Asynchronously chains a function that returns a non-generic <see cref="Result"/>.
    /// If the result has failed, the error is propagated without invoking <paramref name="binder"/>.
    /// On success, the original value is preserved.
    /// </summary>
    /// <typeparam name="T">The type of the current value.</typeparam>
    /// <param name="result">The result to bind over.</param>
    /// <param name="binder">An async function to apply to the success value.</param>
    public static async Task<Result<T>> BindAsync<T>(
        this Result<T> result, Func<T, Task<Result>> binder)
    {
      if (result.IsFailure) return result;
      var next = await binder(result.Value);
      return next.IsSuccess ? result : Result<T>.Fail(next.Error);
    }

    // ── Task<Result> Passthrough ─────────────────────────────────────────

    /// <summary>
    /// Maps over a <see cref="Task"/>-wrapped <see cref="Result{T}"/> using <paramref name="mapper"/>.
    /// </summary>
    /// <typeparam name="T">The type of the current value.</typeparam>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <param name="task">The task wrapping the result.</param>
    /// <param name="mapper">A function to apply to the success value.</param>
    public static async Task<Result<TOut>> Map<T, TOut>(
        this Task<Result<T>> task, Func<T, TOut> mapper)
    {
      var result = await task;
      return result.Map(mapper);
    }

    /// <summary>
    /// Binds over a <see cref="Task"/>-wrapped <see cref="Result{T}"/> using <paramref name="binder"/>.
    /// </summary>
    /// <typeparam name="T">The type of the current value.</typeparam>
    /// <typeparam name="TOut">The type of the next result's value.</typeparam>
    /// <param name="task">The task wrapping the result.</param>
    /// <param name="binder">A function to apply to the success value.</param>
    public static async Task<Result<TOut>> Bind<T, TOut>(
        this Task<Result<T>> task, Func<T, Result<TOut>> binder)
    {
      var result = await task;
      return result.Bind(binder);
    }

    /// <summary>
    /// Matches over a <see cref="Task"/>-wrapped <see cref="Result{T}"/>,
    /// returning a value of type <typeparamref name="TOut"/>.
    /// </summary>
    /// <typeparam name="T">The type of the current value.</typeparam>
    /// <typeparam name="TOut">The return type.</typeparam>
    /// <param name="task">The task wrapping the result.</param>
    /// <param name="onSuccess">Invoked with the value when the result is successful.</param>
    /// <param name="onFailure">Invoked with the error when the result has failed.</param>
    public static async Task<TOut> Match<T, TOut>(
        this Task<Result<T>> task, Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
      var result = await task;
      return result.Match(onSuccess, onFailure);
    }

    // ── Combine ─────────────────────────────────────────────────────────

    /// <summary>
    /// Combines multiple non-generic <see cref="Result"/> instances into one.
    /// Returns <see cref="Result.Ok"/> if all succeed.
    /// If any fail, returns the first error with subsequent errors chained as causes.
    /// </summary>
    /// <param name="results">The results to combine.</param>
    public static Result Combine(params Result[] results)
    {
      var errors = results.Where(r => r.IsFailure).Select(r => r.Error).ToList();
      if (!errors.Any()) return Result.Ok();

      var combined = errors[0];
      for (int i = 1; i < errors.Count; i++)
        combined = combined.WithCause(errors[i]);

      return Result.Fail(combined);
    }

    /// <summary>
    /// Combines multiple generic <see cref="Result{T}"/> instances into a single result containing all values.
    /// Supports inline usage via <c>params</c>: <c>Combine(r1, r2, r3)</c>.
    /// Returns <see cref="Result.Fail{T}(Error)"/> if any result has failed.
    /// </summary>
    /// <typeparam name="T">The type of each result's value.</typeparam>
    /// <param name="results">The results to combine.</param>
    public static Result<IReadOnlyList<T>> Combine<T>(params Result<T>[] results)
        => Combine<T>((IEnumerable<Result<T>>)results);

    /// <summary>
    /// Combines a sequence of <see cref="Result{T}"/> instances into a single result containing all values.
    /// Returns <see cref="Result.Fail{T}(Error)"/> if any result has failed.
    /// </summary>
    /// <typeparam name="T">The type of each result's value.</typeparam>
    /// <param name="results">The sequence of results to combine.</param>
    public static Result<IReadOnlyList<T>> Combine<T>(IEnumerable<Result<T>> results)
    {
      var list = results.ToList();
      var failures = list.Where(r => r.IsFailure).Select(r => r.Error).ToList();

      if (failures.Any())
      {
        var combined = failures[0];
        for (int i = 1; i < failures.Count; i++)
          combined = combined.WithCause(failures[i]);
        return Result<IReadOnlyList<T>>.Fail(combined);
      }

      return Result<IReadOnlyList<T>>.Ok(list.Select(r => r.Value).ToList());
    }

    // ── CombineAsync ─────────────────────────────────────────────────────

    /// <summary>
    /// Awaits multiple async <see cref="Result{T}"/> tasks in parallel and combines them into one.
    /// Ideal for parallel API calls or independent async operations.
    /// Returns <see cref="Result.Fail{T}(Error)"/> if any result has failed.
    /// </summary>
    /// <typeparam name="T">The type of each result's value.</typeparam>
    /// <param name="tasks">The async result tasks to await and combine.</param>
    public static async Task<Result<IReadOnlyList<T>>> CombineAsync<T>(
        params Task<Result<T>>[] tasks)
    {
      var results = await Task.WhenAll(tasks);
      return Combine<T>(results);
    }

    /// <summary>
    /// Awaits a sequence of async <see cref="Result{T}"/> tasks in parallel and combines them into one.
    /// Returns <see cref="Result.Fail{T}(Error)"/> if any result has failed.
    /// </summary>
    /// <typeparam name="T">The type of each result's value.</typeparam>
    /// <param name="tasks">The sequence of async result tasks to await and combine.</param>
    public static async Task<Result<IReadOnlyList<T>>> CombineAsync<T>(
        IEnumerable<Task<Result<T>>> tasks)
    {
      var results = await Task.WhenAll(tasks);
      return Combine<T>(results);
    }

    // ── Ensure ───────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the success value against <paramref name="predicate"/>.
    /// Returns a failure with <paramref name="errorMessage"/> if the predicate is not met.
    /// If the result has already failed, it is returned unchanged.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="predicate">The condition the value must satisfy.</param>
    /// <param name="errorMessage">The error message to use when the predicate fails.</param>
    public static Result<T> Ensure<T>(
        this Result<T> result, Func<T, bool> predicate, string errorMessage)
    {
      if (result.IsFailure) return result;
      return predicate(result.Value)
          ? result
          : Result<T>.Fail(new Error(errorMessage, code: "ENSURE_FAILED"));
    }

    /// <summary>
    /// Validates the success value against <paramref name="predicate"/>.
    /// Returns a failure with <paramref name="error"/> if the predicate is not met.
    /// If the result has already failed, it is returned unchanged.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="predicate">The condition the value must satisfy.</param>
    /// <param name="error">The error to use when the predicate fails.</param>
    public static Result<T> Ensure<T>(
        this Result<T> result, Func<T, bool> predicate, Error error)
    {
      if (result.IsFailure) return result;
      return predicate(result.Value) ? result : Result<T>.Fail(error);
    }

    // ── Try ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Wraps a function that may throw an exception into a <see cref="Result{T}"/>.
    /// If an exception is thrown, it is converted using <paramref name="errorMapper"/>
    /// or wrapped in an <c>UNEXPECTED_ERROR</c> by default.
    /// </summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="errorMapper">An optional function to convert the exception into an <see cref="Error"/>.</param>
    public static Result<T> Try<T>(Func<T> func, Func<Exception, Error>? errorMapper = null)
    {
      try
      {
        return Result<T>.Ok(func());
      }
      catch (Exception ex)
      {
        return Result<T>.Fail(errorMapper?.Invoke(ex) ?? Error.Unexpected(ex));
      }
    }

    /// <summary>
    /// Wraps a void action that may throw an exception into a non-generic <see cref="Result"/>.
    /// If an exception is thrown, it is converted using <paramref name="errorMapper"/>
    /// or wrapped in an <c>UNEXPECTED_ERROR</c> by default.
    /// </summary>
    /// <param name="action">The action to execute.</param>
    /// <param name="errorMapper">An optional function to convert the exception into an <see cref="Error"/>.</param>
    public static Result Try(Action action, Func<Exception, Error>? errorMapper = null)
    {
      try
      {
        action();
        return Result.Ok();
      }
      catch (Exception ex)
      {
        return Result.Fail(errorMapper?.Invoke(ex) ?? Error.Unexpected(ex));
      }
    }

    /// <summary>
    /// Wraps an async function that may throw an exception into a <see cref="Task"/>-wrapped <see cref="Result{T}"/>.
    /// If an exception is thrown, it is converted using <paramref name="errorMapper"/>
    /// or wrapped in an <c>UNEXPECTED_ERROR</c> by default.
    /// </summary>
    /// <typeparam name="T">The type of the return value.</typeparam>
    /// <param name="func">The async function to execute.</param>
    /// <param name="errorMapper">An optional function to convert the exception into an <see cref="Error"/>.</param>
    public static async Task<Result<T>> TryAsync<T>(
        Func<Task<T>> func, Func<Exception, Error>? errorMapper = null)
    {
      try
      {
        return Result<T>.Ok(await func());
      }
      catch (Exception ex)
      {
        return Result<T>.Fail(errorMapper?.Invoke(ex) ?? Error.Unexpected(ex));
      }
    }
  }
}