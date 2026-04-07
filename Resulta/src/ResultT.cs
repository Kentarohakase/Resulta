using System;

namespace Resulta
{
  /// <summary>
  /// Represents a result with a return value — either a value (success) or an error (failure).
  /// Use <see cref="Ok"/> to create a successful result and <see cref="Fail(string)"/> to create a failed one.
  /// </summary>
  /// <typeparam name="T">The type of the success value.</typeparam>
  public sealed class Result<T>
  {
    private readonly T? _value;
    private readonly Error? _error;

    /// <summary>Gets a value indicating whether the result is successful.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the result has failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result has failed and has no value.</exception>
    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException($"No value present – result has failed: {Error}");

    /// <summary>
    /// Gets the error associated with this result.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is successful and has no error.</exception>
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

    /// <summary>Creates a successful result with the given value.</summary>
    /// <param name="value">The success value.</param>
    public static Result<T> Ok(T value) => new Result<T>(value, null, true);

    /// <summary>Creates a failed result with the given error message.</summary>
    /// <param name="message">The error message describing the failure.</param>
    public static Result<T> Fail(string message) => new Result<T>(default, new Error(message), false);

    /// <summary>Creates a failed result with the given error.</summary>
    /// <param name="error">The structured error describing the failure.</param>
    public static Result<T> Fail(Error error)
    {
      ArgumentNullException.ThrowIfNull(error);
      return new Result<T>(default, error, false);
    }

    // ── Map ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Transforms the success value using <paramref name="mapper"/>.
    /// If the result has failed, the error is propagated without invoking the mapper.
    /// </summary>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <param name="mapper">The function to apply to the success value.</param>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
    {
      ArgumentNullException.ThrowIfNull(mapper);
      return IsSuccess
          ? Result<TOut>.Ok(mapper(Value))
          : Result<TOut>.Fail(Error);
    }

    /// <summary>
    /// Transforms the success value into another <see cref="Result{TOut}"/> using <paramref name="mapper"/>.
    /// If the result has failed, the error is propagated without invoking the mapper.
    /// </summary>
    /// <typeparam name="TOut">The type of the transformed value.</typeparam>
    /// <param name="mapper">The function to apply to the success value, returning a new result.</param>
    public Result<TOut> Map<TOut>(Func<T, Result<TOut>> mapper)
    {
      ArgumentNullException.ThrowIfNull(mapper);
      return IsSuccess
          ? mapper(Value)
          : Result<TOut>.Fail(Error);
    }

    // ── Bind ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Chains a function that returns a <see cref="Result{TOut}"/>.
    /// Failures are propagated automatically without invoking <paramref name="binder"/>.
    /// </summary>
    /// <typeparam name="TOut">The type of the next result's value.</typeparam>
    /// <param name="binder">The function to apply to the success value.</param>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
    {
      ArgumentNullException.ThrowIfNull(binder);
      return IsSuccess
          ? binder(Value)
          : Result<TOut>.Fail(Error);
    }

    /// <summary>
    /// Chains a function that returns a non-generic <see cref="Result"/>.
    /// Failures are propagated automatically without invoking <paramref name="binder"/>.
    /// </summary>
    /// <param name="binder">The function to apply to the success value.</param>
    public Result Bind(Func<T, Result> binder)
    {
      ArgumentNullException.ThrowIfNull(binder);
      return IsSuccess
          ? binder(Value)
          : Result.Fail(Error);
    }

    // ── Match ────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles both the success and failure case and returns a value of type <typeparamref name="TOut"/>.
    /// </summary>
    /// <typeparam name="TOut">The return type.</typeparam>
    /// <param name="onSuccess">Invoked with the value when the result is successful.</param>
    /// <param name="onFailure">Invoked with the error when the result has failed.</param>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
    {
      ArgumentNullException.ThrowIfNull(onSuccess);
      ArgumentNullException.ThrowIfNull(onFailure);
      return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }

    /// <summary>
    /// Handles both the success and failure case without returning a value.
    /// </summary>
    /// <param name="onSuccess">Invoked with the value when the result is successful.</param>
    /// <param name="onFailure">Invoked with the error when the result has failed.</param>
    public void Match(Action<T> onSuccess, Action<Error> onFailure)
    {
      ArgumentNullException.ThrowIfNull(onSuccess);
      ArgumentNullException.ThrowIfNull(onFailure);
      if (IsSuccess) onSuccess(Value);
      else onFailure(Error);
    }

    // ── Tap ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Executes <paramref name="action"/> if the result is successful. Returns the same result unchanged.
    /// </summary>
    /// <param name="action">The side effect to execute on success, receiving the value.</param>
    public Result<T> OnSuccess(Action<T> action)
    {
      ArgumentNullException.ThrowIfNull(action);
      if (IsSuccess) action(Value);
      return this;
    }

    /// <summary>
    /// Executes <paramref name="action"/> if the result has failed. Returns the same result unchanged.
    /// </summary>
    /// <param name="action">The side effect to execute on failure, receiving the error.</param>
    public Result<T> OnFailure(Action<Error> action)
    {
      ArgumentNullException.ThrowIfNull(action);
      if (IsFailure) action(Error);
      return this;
    }

    // ── Fallback ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the success value if the result is successful, otherwise returns <paramref name="defaultValue"/>.
    /// </summary>
    /// <param name="defaultValue">The fallback value to return on failure.</param>
    public T GetValueOrDefault(T defaultValue = default!) =>
        IsSuccess ? Value : defaultValue;

    /// <summary>
    /// Returns the success value if the result is successful,
    /// otherwise computes a fallback value from the error using <paramref name="fallback"/>.
    /// </summary>
    /// <param name="fallback">A function that receives the error and returns a fallback value.</param>
    public T GetValueOrElse(Func<Error, T> fallback)
    {
      ArgumentNullException.ThrowIfNull(fallback);
      return IsSuccess ? Value : fallback(Error);
    }

    // ── Implicit Conversions ─────────────────────────────────────────────

    /// <summary>Implicitly converts a value of type <typeparamref name="T"/> to a successful <see cref="Result{T}"/>.</summary>
    /// <param name="value">The value to wrap.</param>
    public static implicit operator Result<T>(T value) => Ok(value);

    /// <summary>Implicitly converts an <see cref="Error"/> to a failed <see cref="Result{T}"/>.</summary>
    /// <param name="error">The error to wrap.</param>
    public static implicit operator Result<T>(Error error) => Fail(error);

    // ── Convert to non-generic Result ────────────────────────────────────

    /// <summary>
    /// Converts this result to a non-generic <see cref="Result"/>, discarding the value on success.
    /// </summary>
    public Result ToResult() =>
        IsSuccess ? Result.Ok() : Result.Fail(Error);

    /// <inheritdoc/>
    public override string ToString() =>
        IsSuccess
            ? $"Result<{typeof(T).Name}> {{ Ok: {_value} }}"
            : $"Result<{typeof(T).Name}> {{ Fail: {Error} }}";
  }
}