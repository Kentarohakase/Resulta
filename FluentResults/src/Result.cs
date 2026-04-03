using System;

namespace FluentResults
{
    /// <summary>
    /// Represents a result without a return value (success or failure).
    /// </summary>
    public readonly struct Result
    {
        private readonly Error? _error;

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;

        public Error Error => IsFailure
            ? _error!
            : throw new InvalidOperationException("No error present – result is successful.");

        private Result(bool isSuccess, Error? error)
        {
            IsSuccess = isSuccess;
            _error = error;
        }

        // ── Factory Methods ──────────────────────────────────────────────────

        public static Result Ok() => new Result(true, null);

        public static Result Fail(string message) => new Result(false, new Error(message));

        public static Result Fail(Error error) => new Result(false, error);

        public static Result<T> Ok<T>(T value) => Result<T>.Ok(value);

        public static Result<T> Fail<T>(string message) => Result<T>.Fail(message);

        public static Result<T> Fail<T>(Error error) => Result<T>.Fail(error);

        // ── Match ────────────────────────────────────────────────────────────

        /// <summary>Handles both success and failure cases and returns a value.</summary>
        public TOut Match<TOut>(Func<TOut> onSuccess, Func<Error, TOut> onFailure)
            => IsSuccess ? onSuccess() : onFailure(_error!);

        /// <summary>Handles both success and failure cases without returning a value.</summary>
        public void Match(Action onSuccess, Action<Error> onFailure)
        {
            if (IsSuccess) onSuccess();
            else onFailure(_error!);
        }

        // ── Tap ─────────────────────────────────────────────────────────────

        /// <summary>Executes an action if the result is successful, without changing the result.</summary>
        public Result OnSuccess(Action action)
        {
            if (IsSuccess) action();
            return this;
        }

        /// <summary>Executes an action if the result has failed, without changing the result.</summary>
        public Result OnFailure(Action<Error> action)
        {
            if (IsFailure) action(_error!);
            return this;
        }

        // ── Implicit Conversions ─────────────────────────────────────────────

        public static implicit operator Result(Error error) => Fail(error);

        public override string ToString()
            => IsSuccess ? "Result { Success }" : $"Result {{ Failure: {_error} }}";
    }
}
