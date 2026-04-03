using System;
using System.Collections.Generic;
using System.Linq;

namespace FluentResults.Extensions
{
    /// <summary>
    /// A result that can collect multiple errors at once – ideal for form validation.
    /// </summary>
    public readonly struct ValidationResult<T>
    {
        private readonly T? _value;
        private readonly IReadOnlyList<Error> _errors;

        public bool IsSuccess => _errors.Count == 0;
        public bool IsFailure => !IsSuccess;

        public T Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("No value present – validation has failed.");

        public IReadOnlyList<Error> Errors => _errors;

        /// <summary>Returns the first error (for compatibility with Result&lt;T&gt;).</summary>
        public Error Error => _errors.FirstOrDefault()
            ?? throw new InvalidOperationException("No errors present.");

        private ValidationResult(T? value, IReadOnlyList<Error> errors)
        {
            _value = value;
            _errors = errors;
        }

        // ── Factory ──────────────────────────────────────────────────────────

        public static ValidationResult<T> Ok(T value)
            => new ValidationResult<T>(value, Array.Empty<Error>());

        public static ValidationResult<T> Fail(params Error[] errors)
            => new ValidationResult<T>(default, errors);

        public static ValidationResult<T> Fail(IEnumerable<Error> errors)
            => new ValidationResult<T>(default, errors.ToList());

        // ── Match ────────────────────────────────────────────────────────────

        /// <summary>Handles both success and failure cases, returning all collected errors on failure.</summary>
        public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
            => IsSuccess ? onSuccess(_value!) : onFailure(_errors);

        // ── Convert to Result ────────────────────────────────────────────────

        /// <summary>Converts this ValidationResult to a regular Result, chaining all errors as causes.</summary>
        public Result<T> ToResult()
        {
            if (IsSuccess) return Result<T>.Ok(_value!);

            var root = _errors[0];
            for (int i = 1; i < _errors.Count; i++)
                root = root.WithCause(_errors[i]);
            return Result<T>.Fail(root);
        }

        public override string ToString()
            => IsSuccess
                ? $"ValidationResult<{typeof(T).Name}> {{ Ok }}"
                : $"ValidationResult<{typeof(T).Name}> {{ {_errors.Count} error(s): {string.Join(", ", _errors.Select(e => e.Message))} }}";
    }

    // ── Validator Builder ─────────────────────────────────────────────────────

    /// <summary>
    /// Fluent builder for collecting validation errors against a value.
    /// </summary>
    public class Validator<T>
    {
        private readonly T _value;
        private readonly List<Error> _errors = new();

        public Validator(T value) => _value = value;

        /// <summary>Adds an error if the predicate is not satisfied.</summary>
        public Validator<T> Must(Func<T, bool> predicate, string errorMessage, string? code = null)
        {
            if (!predicate(_value))
                _errors.Add(new Error(errorMessage, code));
            return this;
        }

        /// <summary>Adds the given error if the predicate is not satisfied.</summary>
        public Validator<T> Must(Func<T, bool> predicate, Error error)
        {
            if (!predicate(_value))
                _errors.Add(error);
            return this;
        }

        /// <summary>Adds an error if the predicate is satisfied (inverse of Must).</summary>
        public Validator<T> MustNot(Func<T, bool> predicate, string errorMessage, string? code = null)
            => Must(v => !predicate(v), errorMessage, code);

        /// <summary>Runs all validations and returns a ValidationResult.</summary>
        public ValidationResult<T> Validate()
            => _errors.Count == 0
                ? ValidationResult<T>.Ok(_value)
                : ValidationResult<T>.Fail(_errors);

        public static Validator<T> For(T value) => new Validator<T>(value);
    }

    // ── Usage Example ─────────────────────────────────────────────────────────
    /*
    record RegisterDto(string Name, string Email, int Age);

    static ValidationResult<RegisterDto> Validate(RegisterDto dto) =>
        Validator<RegisterDto>.For(dto)
            .Must(d => d.Name.Length >= 2,       Error.Validation("name",  "Must be at least 2 characters"))
            .Must(d => d.Email.Contains('@'),     Error.Validation("email", "Must be a valid email address"))
            .Must(d => d.Age >= 18,              Error.Validation("age",   "Must be at least 18 years old"))
            .MustNot(d => d.Name.Contains(' '),  Error.Validation("name",  "Must not contain spaces"))
            .Validate();

    var result = Validate(new RegisterDto("", "not-an-email", 15));

    result.Match(
        onSuccess: dto    => Console.WriteLine($"Registered: {dto.Name}"),
        onFailure: errors =>
        {
            Console.WriteLine($"{errors.Count} error(s) found:");
            foreach (var e in errors)
                Console.WriteLine($"  x {e}");
        }
    );
    // Output:
    // 3 error(s) found:
    //   x Validation failed for 'name': Must be at least 2 characters [VALIDATION_ERROR]
    //   x Validation failed for 'email': Must be a valid email address [VALIDATION_ERROR]
    //   x Validation failed for 'age': Must be at least 18 years old [VALIDATION_ERROR]
    */
}
