using System;

namespace Resulta
{
  /// <summary>
  /// Represents a result without a return value — either a success or a failure.
  /// Use <see cref="Ok"/> to create a successful result and <see cref="Fail(string)"/> to create a failed one.
  /// </summary>
  public sealed class Result
  {
    private readonly Error? _error;

    /// <summary>Gets a value indicating whether the result is successful.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the result has failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error associated with this result.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is successful and has no error.</exception>
    public Error Error =>
        IsFailure
            ? _error ?? throw new InvalidOperationException("No error present – result has failed in an invalid state.")
            : throw new InvalidOperationException("No error present – result is successful.");

    private Result(bool isSuccess, Error? error)
    {
      if (isSuccess && error is not null)
        throw new ArgumentException("A successful result cannot contain an error.", nameof(error));

      if (!isSuccess && error is null)
        throw new ArgumentNullException(nameof(error), "A failed result must contain an error.");

      IsSuccess = isSuccess;
      _error = error;
    }

    // ── Factory Methods ──────────────────────────────────────────────────

    /// <summary>Creates a successful result with no value.</summary>
    public static Result Ok() => new Result(true, null);

    /// <summary>Creates a failed result with the given error message.</summary>
    /// <param name="message">The error message describing the failure.</param>
    public static Result Fail(string message) => new Result(false, new Error(message));

    /// <summary>Creates a failed result with the given error.</summary>
    /// <param name="error">The structured error describing the failure.</param>
    public static Result Fail(Error error)
    {
      ArgumentNullException.ThrowIfNull(error);
      return new Result(false, error);
    }

    // ── OkIf / FailIf ────────────────────────────────────────────────────

    /// <summary>
    /// Returns <see cref="Ok"/> if <paramref name="condition"/> is <c>true</c>,
    /// otherwise returns <see cref="Fail(string)"/> with the given error message.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="errorMessage">The error message to use when the condition is false.</param>
    public static Result OkIf(bool condition, string errorMessage)
        => condition ? Ok() : Fail(errorMessage);

    /// <summary>
    /// Returns <see cref="Ok"/> if <paramref name="condition"/> is <c>true</c>,
    /// otherwise returns <see cref="Fail(Error)"/> with the given error.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="error">The error to use when the condition is false.</param>
    public static Result OkIf(bool condition, Error error)
        => condition ? Ok() : Fail(error);

    /// <summary>
    /// Returns <see cref="Fail(string)"/> with the given error message if <paramref name="condition"/> is <c>true</c>,
    /// otherwise returns <see cref="Ok"/>.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="errorMessage">The error message to use when the condition is true.</param>
    public static Result FailIf(bool condition, string errorMessage)
        => condition ? Fail(errorMessage) : Ok();

    /// <summary>
    /// Returns <see cref="Fail(Error)"/> with the given error if <paramref name="condition"/> is <c>true</c>,
    /// otherwise returns <see cref="Ok"/>.
    /// </summary>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="error">The error to use when the condition is true.</param>
    public static Result FailIf(bool condition, Error error)
        => condition ? Fail(error) : Ok();

    // ── Generic convenience factory methods ──────────────────────────────

    /// <summary>Creates a successful <see cref="Result{T}"/> with the given value.</summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The success value.</param>
    /// <remarks>
    /// For reference types <typeparamref name="T"/>, <paramref name="value"/> may be <c>null</c>; the result is still successful.
    /// If <c>null</c> is invalid in your domain, validate explicitly and return <see cref="Fail{T}(Error)"/> instead of using <see cref="Ok{T}"/>.
    /// </remarks>
    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

    /// <summary>Creates a failed <see cref="Result{T}"/> with the given error message.</summary>
    /// <typeparam name="T">The type of the expected value.</typeparam>
    /// <param name="message">The error message describing the failure.</param>
    public static Result<T> Fail<T>(string message) => Result<T>.Fail(message);

    /// <summary>Creates a failed <see cref="Result{T}"/> with the given error.</summary>
    /// <typeparam name="T">The type of the expected value.</typeparam>
    /// <param name="error">The structured error describing the failure.</param>
    public static Result<T> Fail<T>(Error error)
    {
      ArgumentNullException.ThrowIfNull(error);
      return Result<T>.Fail(error);
    }

    // ── OkIf / FailIf (generic) ──────────────────────────────────────────

    /// <summary>
    /// Returns <see cref="Ok{T}"/> with the given value if <paramref name="condition"/> is <c>true</c>,
    /// otherwise returns <see cref="Fail{T}(string)"/> with the given error message.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="value">The value to return on success.</param>
    /// <param name="errorMessage">The error message to use when the condition is false.</param>
    public static Result<T> OkIf<T>(bool condition, T value, string errorMessage)
        => condition ? Ok(value) : Fail<T>(errorMessage);

    /// <summary>
    /// Returns <see cref="Ok{T}"/> with the given value if <paramref name="condition"/> is <c>true</c>,
    /// otherwise returns <see cref="Fail{T}(Error)"/> with the given error.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="value">The value to return on success.</param>
    /// <param name="error">The error to use when the condition is false.</param>
    public static Result<T> OkIf<T>(bool condition, T value, Error error)
        => condition ? Ok(value) : Fail<T>(error);

    /// <summary>
    /// Returns <see cref="Fail{T}(string)"/> with the given error message if <paramref name="condition"/> is <c>true</c>,
    /// otherwise returns <see cref="Ok{T}"/> with the given value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="value">The value to return on success.</param>
    /// <param name="errorMessage">The error message to use when the condition is true.</param>
    public static Result<T> FailIf<T>(bool condition, T value, string errorMessage)
        => condition ? Fail<T>(errorMessage) : Ok(value);

    /// <summary>
    /// Returns <see cref="Fail{T}(Error)"/> with the given error if <paramref name="condition"/> is <c>true</c>,
    /// otherwise returns <see cref="Ok{T}"/> with the given value.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="condition">The condition to evaluate.</param>
    /// <param name="value">The value to return on success.</param>
    /// <param name="error">The error to use when the condition is true.</param>
    public static Result<T> FailIf<T>(bool condition, T value, Error error)
        => condition ? Fail<T>(error) : Ok(value);

    // ── Match ────────────────────────────────────────────────────────────

    /// <summary>
    /// Handles both the success and failure case and returns a value of type <typeparamref name="TOut"/>.
    /// </summary>
    /// <typeparam name="TOut">The return type.</typeparam>
    /// <param name="onSuccess">Invoked when the result is successful.</param>
    /// <param name="onFailure">Invoked with the error when the result has failed.</param>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
    {
      ArgumentNullException.ThrowIfNull(onSuccess);
      ArgumentNullException.ThrowIfNull(onFailure);
      return IsSuccess ? onSuccess() : onFailure(Error);
    }

    /// <summary>
    /// Handles both the success and failure case without returning a value.
    /// </summary>
    /// <param name="onSuccess">Invoked when the result is successful.</param>
    /// <param name="onFailure">Invoked with the error when the result has failed.</param>
    public void Match(Action onSuccess, Action<Error> onFailure)
    {
      ArgumentNullException.ThrowIfNull(onSuccess);
      ArgumentNullException.ThrowIfNull(onFailure);
      if (IsSuccess) onSuccess();
      else onFailure(Error);
    }

    /// <summary>
    /// Executes <paramref name="action"/> if the result is successful. Returns the same result unchanged.
    /// </summary>
    /// <param name="action">The side effect to execute on success.</param>
    public Result OnSuccess(Action action)
    {
      ArgumentNullException.ThrowIfNull(action);
      if (IsSuccess) action();
      return this;
    }

    /// <summary>
    /// Executes <paramref name="action"/> if the result has failed. Returns the same result unchanged.
    /// </summary>
    /// <param name="action">The side effect to execute on failure, receiving the error.</param>
    public Result OnFailure(Action<Error> action)
    {
      ArgumentNullException.ThrowIfNull(action);
      if (IsFailure) action(Error);
      return this;
    }

    /// <summary>Implicitly converts an <see cref="Error"/> to a failed <see cref="Result"/>.</summary>
    /// <param name="error">The error to wrap.</param>
    public static implicit operator Result(Error error) => Fail(error);

    /// <inheritdoc/>
    public override string ToString() =>
        IsSuccess
            ? "Result { Success }"
            : $"Result {{ Failure: {Error} }}";
  }
}