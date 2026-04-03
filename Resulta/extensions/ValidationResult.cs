using System;
using System.Collections.Generic;
using System.Linq;

using Resulta;

namespace Resulta.Extensions
{
  /// <summary>
  /// A result that can collect multiple errors at once – ideal for form validation.
  /// </summary>
  public sealed class ValidationResult<T>
  {
    private readonly T? _value;
    private readonly IReadOnlyList<Error> _errors;

    public bool IsSuccess => _errors.Count == 0;
    public bool IsFailure => !IsSuccess;

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("No value present – validation has failed.");

    public IReadOnlyList<Error> Errors => _errors;

    /// <summary>
    /// Returns the first error for compatibility with Result&lt;T&gt;-style handling.
    /// </summary>
    public Error Error =>
        _errors.FirstOrDefault()
        ?? throw new InvalidOperationException("No errors present.");

    private ValidationResult(T? value, IReadOnlyList<Error>? errors)
    {
      _value = value;
      _errors = errors ?? Array.Empty<Error>();
    }

    public static ValidationResult<T> Ok(T value) =>
        new ValidationResult<T>(value, Array.Empty<Error>());

    public static ValidationResult<T> Fail(params Error[] errors) =>
        new ValidationResult<T>(default, NormalizeErrors(errors));

    public static ValidationResult<T> Fail(IEnumerable<Error> errors) =>
        new ValidationResult<T>(default, NormalizeErrors(errors));

    /// <summary>
    /// Handles both success and failure cases, returning all collected errors on failure.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IReadOnlyList<Error>, TOut> onFailure)
    {
      ArgumentNullException.ThrowIfNull(onSuccess);
      ArgumentNullException.ThrowIfNull(onFailure);

      return IsSuccess ? onSuccess(Value) : onFailure(_errors);
    }

    /// <summary>
    /// Converts this ValidationResult to a regular Result, chaining all errors as causes.
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
  /// Fluent builder for collecting validation errors against a value.
  /// </summary>
  public sealed class Validator<T>
  {
    private readonly T _value;
    private readonly List<Error> _errors = new();

    public Validator(T value) => _value = value;

    public Validator<T> Must(Func<T, bool> predicate, string errorMessage, string? code = null)
    {
      ArgumentNullException.ThrowIfNull(predicate);

      if (!predicate(_value))
        _errors.Add(new Error(errorMessage, code));

      return this;
    }

    public Validator<T> Must(Func<T, bool> predicate, Error error)
    {
      ArgumentNullException.ThrowIfNull(predicate);
      ArgumentNullException.ThrowIfNull(error);

      if (!predicate(_value))
        _errors.Add(error);

      return this;
    }

    public Validator<T> MustNot(Func<T, bool> predicate, string errorMessage, string? code = null) =>
        Must(v => !predicate(v), errorMessage, code);

    public ValidationResult<T> Validate() =>
        _errors.Count == 0
            ? ValidationResult<T>.Ok(_value)
            : ValidationResult<T>.Fail(_errors);

    public static Validator<T> For(T value) => new Validator<T>(value);
  }
}