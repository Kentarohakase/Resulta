using System;

namespace FluentResults
{
  /// <summary>
  /// Represents a result with a return value – either a value (Ok) or an error (Fail).
  /// </summary>
  public sealed class Result<T>
  {
    private readonly T? _value;
    private readonly Error? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException($"No value present – result has failed: {Error}");

    public Error Error =>
        IsFailure
            ? _error ?? throw new InvalidOperationException("No error present – result has failed in an invalid state.")
            : throw new InvalidOperationException("No error present – result is successful.");

    private Result(T? value, Error? error, bool isSuccess)
    {
      if (isSuccess && error is not null)
        throw new ArgumentException("A successful result cannot contain an error.", nameof(error));

      if (!isSuccess && error is null)
        throw new ArgumentNullException(nameof(error), "A failed result must contain an error.");

      _value = value;
      _error = error;
      IsSuccess = isSuccess;
    }

    // ── Factory Methods ──────────────────────────────────────────────────

    public static Result<T> Ok(T value) => new Result<T>(value, null, true);

    public static Result<T> Fail(string message) => new Result<T>(default, new Error(message), false);

    public static Result<T> Fail(Error error)
    {
      ArgumentNullException.ThrowIfNull(error);
      return new Result<T>(default, error, false);
    }

    // ── Map ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Transforms the value using the given mapper function if the result is successful.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
      ArgumentNullException.ThrowIfNull(mapper);

      return IsSuccess
          ? Result<TOut>.Ok(mapper(Value))
          : Result<TOut>.Fail(Error);
    }

    /// <summary>
    /// Transforms the value into another Result if successful.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, Result<TOut>> mapper)
    {
      ArgumentNullException.ThrowIfNull(mapper);

      return IsSuccess
          ? mapper(Value)
          : Result<TOut>.Fail(Error);
    }

    // ── Bind ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Chains a function that returns a Result, propagating failures automatically.
    /// </summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
    {
      ArgumentNullException.ThrowIfNull(binder);

      return IsSuccess
          ? binder(Value)
          : Result<TOut>.Fail(Error);
    }

    /// <summary>
    /// Chains a function that returns a non-generic Result.
    /// </summary>
    public Result Bind(Func<T, Result> binder)
    {
      ArgumentNullException.ThrowIfNull(binder);

      return IsSuccess
          ? binder(Value)
          : Result.Fail(Error);
    }

    // ── Match ────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles both success and failure cases and returns a value.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
      ArgumentNullException.ThrowIfNull(onSuccess);
      ArgumentNullException.ThrowIfNull(onFailure);

      return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }

    /// <summary>
    /// Handles both success and failure cases without returning a value.
    /// </summary>
    public void Match(Action<T> onSuccess, Action<Error> onFailure)
    {
      ArgumentNullException.ThrowIfNull(onSuccess);
      ArgumentNullException.ThrowIfNull(onFailure);

      if (IsSuccess)
        onSuccess(Value);
      else
        onFailure(Error);
    }

    // ── Tap ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Executes an action if the result is successful, without changing the result.
    /// </summary>
    public Result<T> OnSuccess(Action<T> action)
    {
      ArgumentNullException.ThrowIfNull(action);

      if (IsSuccess)
        action(Value);

      return this;
    }

    /// <summary>
    /// Executes an action if the result has failed, without changing the result.
    /// </summary>
    public Result<T> OnFailure(Action<Error> action)
    {
      ArgumentNullException.ThrowIfNull(action);

      if (IsFailure)
        action(Error);

      return this;
    }

    // ── Fallback ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the value if successful, otherwise the given default value.
    /// </summary>
    public T GetValueOrDefault(T defaultValue = default!) =>
        IsSuccess ? Value : defaultValue;

    /// <summary>
    /// Returns the value if successful, otherwise computes a fallback from the error.
    /// </summary>
    public T GetValueOrElse(Func<Error, T> fallback)
    {
      ArgumentNullException.ThrowIfNull(fallback);

      return IsSuccess ? Value : fallback(Error);
    }

    // ── Implicit Conversions ─────────────────────────────────────────────

    public static implicit operator Result<T>(T value) => Ok(value);

    public static implicit operator Result<T>(Error error) => Fail(error);

    // ── Convert to non-generic Result ────────────────────────────────────

    /// <summary>
    /// Converts this result to a non-generic Result, discarding the value.
    /// </summary>
    public Result ToResult() =>
        IsSuccess ? Result.Ok() : Result.Fail(Error);

    public override string ToString() =>
        IsSuccess
            ? $"Result<{typeof(T).Name}> {{ Ok: {_value} }}"
            : $"Result<{typeof(T).Name}> {{ Fail: {Error} }}";
  }
}