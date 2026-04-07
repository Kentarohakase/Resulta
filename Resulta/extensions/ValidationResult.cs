using System;
using System.Collections.Generic;
using System.Linq;

using Resulta;

namespace Resulta.Extensions
{
  /// <summary>
  /// A result that collects multiple validation errors at once — ideal for form and API input validation.
  /// Unlike <see cref="Result{T}"/>, which stops at the first error, <see cref="ValidationResult{T}"/>
  /// accumulates all errors so they can be reported together.
  /// </summary>
  /// <typeparam name="T">The type of the validated value.</typeparam>
  public sealed class ValidationResult<T>
  {
    private readonly T? _value;
    private readonly IReadOnlyList<Error> _errors;

    /// <summary>Gets a value indicating whether the validation passed.</summary>
    public bool IsSuccess => _errors.Count == 0;

    /// <summary>Gets a value indicating whether the validation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the validated value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the validation has failed and has no value.</exception>
    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("No value present – validation has failed.");

    /// <summary>Gets the full list of validation errors collected during validation.</summary>
    public IReadOnlyList<Error> Errors => _errors;

    /// <summary>
    /// Gets the first error for compatibility with <see cref="Result{T}"/>-style handling.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when there are no errors.</exception>
    public Error Error =>
        _errors.FirstOrDefault()
        ?? throw new InvalidOperationException("No errors present.");

    private ValidationResult(T? value, IReadOnlyList<Error>? errors)
    {
      _value = value;
      _errors = errors ?? Array.Empty<Error>();
    }

    /// <summary>Creates a successful <see cref="ValidationResult{T}"/> with the given validated value.</summary>
    /// <param name="value">The successfully validated value.</param>
    public static ValidationResult<T> Ok(T value) =>
        new ValidationResult<T>(value, Array.Empty<Error>());

    /// <summary>
    /// Creates a failed <see cref="ValidationResult{T}"/> with one or more errors.
    /// </summary>
    /// <param name="errors">The validation errors to collect. Must contain at least one error.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errors"/> is empty.</exception>
    public static ValidationResult<T> Fail(params Error[] errors) =>
        new ValidationResult<T>(default, NormalizeErrors(errors));

    /// <summary>
    /// Creates a failed <see cref="ValidationResult{T}"/> from a sequence of errors.
    /// </summary>
    /// <param name="errors">The validation errors to collect. Must contain at least one error.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="errors"/> is empty.</exception>
    public static ValidationResult<T> Fail(IEnumerable<Error> errors) =>
        new ValidationResult<T>(default, NormalizeErrors(errors));

    /// <summary>
    /// Handles both the success and failure case, returning all collected errors on failure.
    /// </summary>
    /// <typeparam name="TOut">The return type.</typeparam>
    /// <param name="onSuccess">Invoked with the validated value when validation passed.</param>
    /// <param name="onFailure">Invoked with the full list of errors when validation failed.</param>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
      ArgumentNullException.ThrowIfNull(onSuccess);
      ArgumentNullException.ThrowIfNull(onFailure);
      return IsSuccess ? onSuccess(Value) : onFailure(_errors);
    }

    /// <summary>
    /// Converts this <see cref="ValidationResult{T}"/> to a regular <see cref="Result{T}"/>,
    /// chaining all errors as causes on the first error.
    /// </summary>
    public Result<T> ToResult()
    {
      if (IsSuccess)
        return Result.Ok(Value);

      var root = _errors[0];
      for (int i = 1; i < _errors.Count; i++)
        root = root.WithCause(_errors[i]);

      return Result.Fail<T>(root);
    }

    /// <inheritdoc/>
    public override string ToString() =>
        IsSuccess
            ? $"ValidationResult<{typeof(T).Name}> {{ Ok }}"
            : $"ValidationResult<{typeof(T).Name}> {{ {_errors.Count} error(s): {string.Join(", ", _errors.Select(e => e.Message))} }}";

    private static IReadOnlyList<Error> NormalizeErrors(IEnumerable<Error>? errors)
    {
      ArgumentNullException.ThrowIfNull(errors);

      var list = errors.ToList();

      if (list.Count == 0)
        throw new ArgumentException("A failed validation result must contain at least one error.", nameof(errors));

      if (list.Any(e => e is null))
        throw new ArgumentException("Validation errors must not contain null values.", nameof(errors));

      return list;
    }
  }

  /// <summary>
  /// A fluent builder for collecting validation errors against a value of type <typeparamref name="T"/>.
  /// All rules are evaluated regardless of earlier failures, allowing all errors to be reported at once.
  /// </summary>
  /// <typeparam name="T">The type of the value being validated.</typeparam>
  public sealed class Validator<T>
  {
    private readonly T _value;
    private readonly List<Error> _errors = new();

    /// <summary>
    /// Initializes a new <see cref="Validator{T}"/> for the given value.
    /// Prefer using <see cref="For"/> as the entry point.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    public Validator(T value) => _value = value;

    /// <summary>
    /// Adds a validation rule. If <paramref name="predicate"/> returns <c>false</c>,
    /// a new error with <paramref name="errorMessage"/> is collected.
    /// </summary>
    /// <param name="predicate">The condition the value must satisfy.</param>
    /// <param name="errorMessage">The error message to use when the predicate fails.</param>
    /// <param name="code">An optional error code to attach to the error.</param>
    public Validator<T> Must(Func<T, bool> predicate, string errorMessage, string? code = null)
    {
      ArgumentNullException.ThrowIfNull(predicate);
      if (!predicate(_value))
        _errors.Add(new Error(errorMessage, code));
      return this;
    }

    /// <summary>
    /// Adds a validation rule using a pre-built <see cref="Error"/>.
    /// If <paramref name="predicate"/> returns <c>false</c>, the given error is collected.
    /// </summary>
    /// <param name="predicate">The condition the value must satisfy.</param>
    /// <param name="error">The error to collect when the predicate fails.</param>
    public Validator<T> Must(Func<T, bool> predicate, Error error)
    {
      ArgumentNullException.ThrowIfNull(predicate);
      ArgumentNullException.ThrowIfNull(error);
      if (!predicate(_value))
        _errors.Add(error);
      return this;
    }

    /// <summary>
    /// Adds an inverse validation rule. If <paramref name="predicate"/> returns <c>true</c>,
    /// a new error with <paramref name="errorMessage"/> is collected.
    /// </summary>
    /// <param name="predicate">The condition that must NOT be satisfied.</param>
    /// <param name="errorMessage">The error message to use when the predicate is true.</param>
    /// <param name="code">An optional error code to attach to the error.</param>
    public Validator<T> MustNot(Func<T, bool> predicate, string errorMessage, string? code = null) =>
        Must(v => !predicate(v), errorMessage, code);

    /// <summary>
    /// Evaluates all rules and returns a <see cref="ValidationResult{T}"/> containing
    /// the validated value on success, or all collected errors on failure.
    /// </summary>
    public ValidationResult<T> Validate() =>
        _errors.Count == 0
            ? ValidationResult<T>.Ok(_value)
            : ValidationResult<T>.Fail(_errors);

    /// <summary>
    /// Creates a new <see cref="Validator{T}"/> for the given <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    public static Validator<T> For(T value) => new Validator<T>(value);
  }
}