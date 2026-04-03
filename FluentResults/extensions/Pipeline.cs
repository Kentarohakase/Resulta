using System;
using System.Threading.Tasks;

namespace FluentResults.Extensions
{
    /// <summary>
    /// Railway-Oriented Pipeline – chain processing steps elegantly.
    /// Each step is only executed if the previous one succeeded.
    /// </summary>
    public class Pipeline<T>
    {
        private readonly Result<T> _current;

        private Pipeline(Result<T> result) => _current = result;

        // ── Entry Points ─────────────────────────────────────────────────────

        /// <summary>Starts a pipeline with a plain value.</summary>
        public static Pipeline<T> Start(T value)
            => new Pipeline<T>(Result<T>.Ok(value));

        /// <summary>Starts a pipeline from an existing Result.</summary>
        public static Pipeline<T> Start(Result<T> result)
            => new Pipeline<T>(result);

        // ── Steps ────────────────────────────────────────────────────────────

        /// <summary>Chains a step that returns a Result. Skipped if already failed.</summary>
        public Pipeline<TOut> Then<TOut>(Func<T, Result<TOut>> step)
            => new Pipeline<TOut>(_current.Bind(step));

        /// <summary>Chains a step that returns a plain value. Skipped if already failed.</summary>
        public Pipeline<TOut> Then<TOut>(Func<T, TOut> step)
            => new Pipeline<TOut>(_current.Map(step));

        /// <summary>Validates the current value against a predicate. Fails with the given message if not met.</summary>
        public Pipeline<T> Validate(Func<T, bool> predicate, string errorMessage)
            => new Pipeline<T>(_current.Ensure(predicate, errorMessage));

        /// <summary>Validates the current value against a predicate. Fails with the given error if not met.</summary>
        public Pipeline<T> Validate(Func<T, bool> predicate, Error error)
            => new Pipeline<T>(_current.Ensure(predicate, error));

        /// <summary>Executes a side effect if the pipeline is still successful.</summary>
        public Pipeline<T> Tap(Action<T> action)
            => new Pipeline<T>(_current.OnSuccess(action));

        // ── Termination ──────────────────────────────────────────────────────

        /// <summary>Terminates the pipeline and handles both success and failure cases.</summary>
        public TOut Finally<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
            => _current.Match(onSuccess, onFailure);

        /// <summary>Terminates the pipeline with void actions for both cases.</summary>
        public void Finally(Action<T> onSuccess, Action<Error> onFailure)
            => _current.Match(onSuccess, onFailure);

        /// <summary>Returns the underlying Result without handling it.</summary>
        public Result<T> Build() => _current;

        // ── Async Bridge ─────────────────────────────────────────────────────

        /// <summary>Transitions to an async pipeline.</summary>
        public AsyncPipeline<T> ThenAsync(Func<T, Task<Result<T>>> step)
            => new AsyncPipeline<T>(Task.FromResult(_current)).ThenAsync(step);
    }

    /// <summary>
    /// Async-capable Railway-Oriented Pipeline.
    /// </summary>
    public class AsyncPipeline<T>
    {
        private readonly Task<Result<T>> _task;

        internal AsyncPipeline(Task<Result<T>> task) => _task = task;

        /// <summary>Starts an async pipeline from a factory function.</summary>
        public static AsyncPipeline<T> Start(Func<Task<Result<T>>> factory)
            => new AsyncPipeline<T>(factory());

        /// <summary>Chains an async step that returns a Result. Skipped if already failed.</summary>
        public AsyncPipeline<TOut> ThenAsync<TOut>(Func<T, Task<Result<TOut>>> step)
        {
            var next = _task.Bind(step);
            return new AsyncPipeline<TOut>(next);
        }

        /// <summary>Chains a synchronous step. Skipped if already failed.</summary>
        public AsyncPipeline<TOut> Then<TOut>(Func<T, Result<TOut>> step)
        {
            var next = _task.Bind(v => Task.FromResult(step(v)));
            return new AsyncPipeline<TOut>(next);
        }

        /// <summary>Terminates the async pipeline and handles both cases.</summary>
        public async Task<TOut> Finally<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure)
            => (await _task).Match(onSuccess, onFailure);

        /// <summary>Terminates the async pipeline with void actions for both cases.</summary>
        public async Task Finally(Action<T> onSuccess, Action<Error> onFailure)
            => (await _task).Match(onSuccess, onFailure);

        /// <summary>Returns the underlying Task-wrapped Result without handling it.</summary>
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

    // ── Usage Example ─────────────────────────────────────────────────────────
    /*
    // Sync
    var response = Pipeline<string>
        .Start(input)
        .Validate(s => s.Length > 0, "Input must not be empty")
        .Then(s => s.Trim())
        .Then(s => FindUser(s))           // returns Result<User>
        .Then(user => CheckPermissions(user)) // returns Result<User>
        .Tap(user => logger.Log($"Logged in: {user.Name}"))
        .Then(user => CreateToken(user))  // returns Result<string>
        .Finally(
            onSuccess: token => $"Token: {token}",
            onFailure: err   => $"Error: {err.Message}"
        );

    // Async
    var result = await AsyncPipeline<Order>
        .Start(() => LoadOrderAsync(id))
        .Then(order => Validate(order))
        .ThenAsync(order => ReserveStockAsync(order))
        .ThenAsync(order => ProcessPaymentAsync(order))
        .ThenAsync(order => SendConfirmationEmailAsync(order))
        .Finally(
            onSuccess: _ => "Order placed successfully!",
            onFailure: e => $"Error: {e.Message}"
        );
    */
}
