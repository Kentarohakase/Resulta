namespace Resulta
{
  /// <summary>
  /// Extension methods for async/await support, LINQ, and common result operations.
  /// </summary>
  public static class ResultExtensions
  {
    // ── Async Map / Bind ─────────────────────────────────────────────────

    /// <summary>Asynchronously transforms the value if the result is successful.</summary>
    public static async Task<Result<TOut>> MapAsync<T, TOut>(
        this Result<T> result, Func<T, Task<TOut>> mapper)
    {
      if (result.IsFailure) return Result<TOut>.Fail(result.Error);
      var mapped = await mapper(result.Value);
      return Result<TOut>.Ok(mapped);
    }

    /// <summary>Asynchronously chains a function that returns a Result.</summary>
    public static async Task<Result<TOut>> BindAsync<T, TOut>(
        this Result<T> result, Func<T, Task<Result<TOut>>> binder)
    {
      if (result.IsFailure) return Result<TOut>.Fail(result.Error);
      return await binder(result.Value);
    }

    public static async Task<Result<T>> BindAsync<T>(
        this Result<T> result, Func<T, Task<Result>> binder)
    {
      if (result.IsFailure) return result;
      var next = await binder(result.Value);
      return next.IsSuccess ? result : Result<T>.Fail(next.Error);
    }

    // ── Task<Result> Passthrough ──────────────────────────────────────────

    /// <summary>Maps over a Task-wrapped Result.</summary>
    public static async Task<Result<TOut>> Map<T, TOut>(
        this Task<Result<T>> task, Func<T, TOut> mapper)
    {
      var result = await task;
      return result.Map(mapper);
    }

    /// <summary>Binds over a Task-wrapped Result.</summary>
    public static async Task<Result<TOut>> Bind<T, TOut>(
        this Task<Result<T>> task, Func<T, Result<TOut>> binder)
    {
      var result = await task;
      return result.Bind(binder);
    }

    /// <summary>Matches over a Task-wrapped Result.</summary>
    public static async Task<TOut> Match<T, TOut>(
        this Task<Result<T>> task, Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
      var result = await task;
      return result.Match(onSuccess, onFailure);
    }

    // ── Combine ─────────────────────────────────────────────────────────

    /// <summary>
    /// Combines multiple non-generic Results into one.
    /// Returns Ok if all succeed, otherwise returns the first failure with subsequent errors as causes.
    /// </summary>
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
    /// Combines multiple generic Results into a single Result containing all values.
    /// Supports params for convenient inline usage: Combine(r1, r2, r3).
    /// Returns Fail if any result has failed.
    /// </summary>
    public static Result<IReadOnlyList<T>> Combine<T>(params Result<T>[] results)
        => Combine<T>((IEnumerable<Result<T>>)results);

    /// <summary>
    /// Combines a sequence of Results into a single Result containing all values.
    /// Returns Fail if any result has failed.
    /// </summary>
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

    // ── Ensure ───────────────────────────────────────────────────────────

    /// <summary>Validates the value against a predicate. Returns Fail if the predicate is not met.</summary>
    public static Result<T> Ensure<T>(
        this Result<T> result, Func<T, bool> predicate, string errorMessage)
    {
      if (result.IsFailure) return result;
      return predicate(result.Value)
          ? result
          : Result<T>.Fail(new Error(errorMessage, code: "ENSURE_FAILED"));
    }

    /// <summary>Validates the value against a predicate. Returns Fail with the given error if not met.</summary>
    public static Result<T> Ensure<T>(
        this Result<T> result, Func<T, bool> predicate, Error error)
    {
      if (result.IsFailure) return result;
      return predicate(result.Value) ? result : Result<T>.Fail(error);
    }

    // ── Try ──────────────────────────────────────────────────────────────

    /// <summary>Wraps a function that may throw an exception into a Result.</summary>
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

    /// <summary>Wraps a void action that may throw an exception into a Result.</summary>
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

    /// <summary>Wraps an async function that may throw an exception into a Task-wrapped Result.</summary>
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