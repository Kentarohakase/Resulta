using System;

namespace Resulta
{
  /// <summary>
  /// Represents a result without a return value (success or failure).
  /// </summary>
  public sealed class Result
  {
    private readonly Error? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

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
    public static Result Ok() => new Result(true, null);

    public static Result Fail(string message) => new Result(false, new Error(message));

    public static Result Fail(Error error)
    {
      ArgumentNullException.ThrowIfNull(error);
      return new Result(false, error);
    }

    // Generic convenience factory methods
    public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

    public static Result<T> Fail<T>(string message) => Result<T>.Fail(message);

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

    public static implicit operator Result(Error error) => Fail(error);

    public override string ToString() =>
        IsSuccess
            ? "Result { Success }"
            : $"Result {{ Failure: {Error} }}";
  }
}