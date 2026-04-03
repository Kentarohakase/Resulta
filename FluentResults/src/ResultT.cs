using System;

namespace FluentResults
{
    /// <summary>
    /// Represents a result with a return value – either a value (Ok) or an error (Fail).
    /// </summary>
    public readonly struct Result<T>
    {
        private readonly T? _value;
        private readonly Error? _error;

        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;

        public T Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException($"No value present – result has failed: {_error}");

        public Error Error => IsFailure
            ? _error!
            : throw new InvalidOperationException("No error present – result is successful.");

        private Result(T? value, Error? error, bool isSuccess)
        {
            _value = value;
            _error = error;
            IsSuccess = isSuccess;
        }

        // ── Factory Methods ──────────────────────────────────────────────────

        public static Result<T> Ok(T value) => new Result<T>(value, null, true);

        public static Result<T> Fail(string message) => new Result<T>(default, new Error(message), false);

        public static Result<T> Fail(Error error) => new Result<T>(default, error, false);

        // ── Map ─────────────────────────────────────────────────────────────
        // Transforms the value if successful

        /// <summary>Transforms the value using the given mapper function if the result is successful.</summary>
        public Result<TOut> Map<TOut>(Func<T, TOut> mapper)
            => IsSuccess ? Result<TOut>.Ok(mapper(_value!)) : Result<TOut>.Fail(_error!);

        /// <summary>Transforms the value into another Result if successful.</summary>
        public Result<TOut> Map<TOut>(Func<T, Result<TOut>> mapper)
            => IsSuccess ? mapper(_value!) : Result<TOut>.Fail(_error!);

        // ── Bind ─────────────────────────────────────────────────────────────
        // Chains operations that themselves return a Result

        /// <summary>Chains a function that returns a Result, propagating failures automatically.</summary>
        public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder)
            => IsSuccess ? binder(_value!) : Result<TOut>.Fail(_error!);

        /// <summary>Chains a function that returns a non-generic Result.</summary>
        public Result Bind(Func<T, Result> binder)
            => IsSuccess ? binder(_value!) : Result.Fail(_error!);

        // ── Match ────────────────────────────────────────────────────────────
        // Unwrap the value – forces handling of both cases

        /// <summary>Handles both success and failure cases and returns a value.</summary>
        public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
            => IsSuccess ? onSuccess(_value!) : onFailure(_error!);

        /// <summary>Handles both success and failure cases without returning a value.</summary>
        public void Match(Action<T> onSuccess, Action<Error> onFailure)
        {
            if (IsSuccess) onSuccess(_value!);
            else onFailure(_error!);
        }

        // ── Tap ─────────────────────────────────────────────────────────────
        // Side effects without changing the value

        /// <summary>Executes an action if the result is successful, without changing the result.</summary>
        public Result<T> OnSuccess(Action<T> action)
        {
            if (IsSuccess) action(_value!);
            return this;
        }

        /// <summary>Executes an action if the result has failed, without changing the result.</summary>
        public Result<T> OnFailure(Action<Error> action)
        {
            if (IsFailure) action(_error!);
            return this;
        }

        // ── Fallback ─────────────────────────────────────────────────────────

        /// <summary>Returns the value if successful, otherwise the given default value.</summary>
        public T GetValueOrDefault(T defaultValue = default!)
            => IsSuccess ? _value! : defaultValue;

        /// <summary>Returns the value if successful, otherwise computes a fallback from the error.</summary>
        public T GetValueOrElse(Func<Error, T> fallback)
            => IsSuccess ? _value! : fallback(_error!);

        // ── Implicit Conversions ─────────────────────────────────────────────

        public static implicit operator Result<T>(T value) => Ok(value);
        public static implicit operator Result<T>(Error error) => Fail(error);

        // ── Convert to non-generic Result ────────────────────────────────────

        /// <summary>Converts this result to a non-generic Result, discarding the value.</summary>
        public Result ToResult()
            => IsSuccess ? Result.Ok() : Result.Fail(_error!);

        public override string ToString()
            => IsSuccess ? $"Result<{typeof(T).Name}> {{ Ok: {_value} }}" : $"Result<{typeof(T).Name}> {{ Fail: {_error} }}";
    }
}
