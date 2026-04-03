using System;

namespace Resulta
{
  /// <summary>
  /// Represents a result without a return value (success or failure).
  /// </summary>
  public sealed class Result
  {
    private readonly Error? _error;

    /// <summary>
    /// Indicates whether the result represents a successful outcome.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the result represents a failed outcome.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error for a failed result. Throws an <see cref="InvalidOperationException"/> if the result is successful.
    /// </summary>
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

    // Non-generic factory methods
    /// <summary>
    /// Creates a successful <see cref="Result"/>.
    /// </summary>
    public static Result Ok() => new Result(true, null);

    /// <summary>
    /// Creates a failed <see cref="Result"/> with the given error message.
    /// </summary>
    public static Result Fail(string message) => new Result(false, new Error(message));

    /// <summary>
    /// Creates a failed <see cref="Result"/> with the given <see cref="Error"/>.
    /// </summary>
    public static Result Fail(Error error)
    {
      ArgumentNullException.ThrowIfNull(error);
      return new Result(false, error);
    }

    // Generic convenience factory methods
    /// <summary>
    /// Creates a successful generic <see cref="Result{T}"/> with the given value.
    /// </summary>
    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

    /// <summary>
    /// Creates a failed generic <see cref="Result{T}"/> with the given error message.
    /// </summary>
    public static Result<T> Fail<T>(string message) => Result<T>.Fail(message);

    /// <summary>
    /// Creates a failed generic <see cref="Result{T}"/> with the given <see cref="Error"/>.
    /// </summary>
    public static Result<T> Fail<T>(Error error)
    {
      ArgumentNullException.ThrowIfNull(error);
      return Result<T>.Fail(error);
    }

    /// <summary>
    /// Handles both success and failure cases and returns a value.
    /// </summary>
    public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
    {
      ArgumentNullException.ThrowIfNull(onSuccess);
      ArgumentNullException.ThrowIfNull(onFailure);

      return IsSuccess ? onSuccess() : onFailure(Error);
    }

    /// <summary>
    /// Handles both success and failure cases without returning a value.
    /// </summary>
    public void Match(Action onSuccess, Action<Error> onFailure)
    {
      ArgumentNullException.ThrowIfNull(onSuccess);
      ArgumentNullException.ThrowIfNull(onFailure);

      if (IsSuccess)
        onSuccess();
      else
        onFailure(Error);
    }

    /// <summary>
    /// Executes an action if the result is successful, without changing the result.
    /// </summary>
    public Result OnSuccess(Action action)
    {
      ArgumentNullException.ThrowIfNull(action);

      if (IsSuccess)
        action();

      return this;
    }

    /// <summary>
    /// Executes an action if the result has failed, without changing the result.
    /// </summary>
    public Result OnFailure(Action<Error> action)
    {
      ArgumentNullException.ThrowIfNull(action);

      if (IsFailure)
        action(Error);

      return this;
    }

    /// <summary>
    /// Implicitly converts an <see cref="Error"/> to a failed <see cref="Result"/>.
    /// </summary>
    public static implicit operator Result(Error error) => Fail(error);

    /// <summary>
    /// Returns a string representation of the result.
    /// </summary>
    public override string ToString() =>
        IsSuccess
            ? "Result { Success }"
            : $"Result {{ Failure: {Error} }}";
  }
}