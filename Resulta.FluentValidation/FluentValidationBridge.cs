using global::FluentValidation;

// Explicitly alias FluentValidation's ValidationResult to avoid ambiguity
// with Resulta.Extensions.ValidationResult<T>
using FVResult = global::FluentValidation.Results.ValidationResult;

using Resulta;
using Resulta.Extensions;

namespace Resulta.FluentValidation
{
  /// <summary>
  /// Bridge between FluentValidation and Resulta.
  /// Provides extension methods to convert FluentValidation results directly into
  /// <see cref="Result{T}"/> and <see cref="ValidationResult{T}"/> types.
  /// </summary>
  public static class FluentValidationBridge
  {
    // ── Sync ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates <paramref name="instance"/> synchronously and returns a <see cref="Result{T}"/> directly.
    /// On success, the result wraps the original instance.
    /// On failure, errors are chained as causes on the first error.
    /// </summary>
    /// <typeparam name="T">The type of the instance being validated.</typeparam>
    /// <param name="validator">The FluentValidation validator to use.</param>
    /// <param name="instance">The instance to validate.</param>
    public static Result<T> ValidateToResult<T>(this IValidator<T> validator, T instance)
    {
      var validationResult = validator.Validate(instance);
      return validationResult.ToResult(instance);
    }

    /// <summary>
    /// Converts a FluentValidation <see cref="FVResult"/> into a Resulta <see cref="Result{T}"/>.
    /// On success, wraps <paramref name="value"/>. On failure, chains all errors as causes.
    /// Attaches <c>attemptedValue</c> and <c>severity</c> as metadata on each error.
    /// </summary>
    /// <typeparam name="T">The type of the validated value.</typeparam>
    /// <param name="validationResult">The FluentValidation result to convert.</param>
    /// <param name="value">The value to wrap on success.</param>
    public static Result<T> ToResult<T>(this FVResult validationResult, T value)
    {
      if (validationResult.IsValid)
        return Result<T>.Ok(value);

      var errors = validationResult.Errors
          .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage)
              .WithMetadata("attemptedValue", f.AttemptedValue ?? "null")
              .WithMetadata("severity", f.Severity.ToString()))
          .ToList();

      var root = errors[0];
      for (int i = 1; i < errors.Count; i++)
        root = root.WithCause(errors[i]);

      return Result<T>.Fail(root);
    }

    /// <summary>
    /// Validates <paramref name="instance"/> synchronously and returns a <see cref="ValidationResult{T}"/>
    /// containing all validation errors at once.
    /// Attaches <c>attemptedValue</c> as metadata on each error.
    /// </summary>
    /// <typeparam name="T">The type of the instance being validated.</typeparam>
    /// <param name="validator">The FluentValidation validator to use.</param>
    /// <param name="instance">The instance to validate.</param>
    public static ValidationResult<T> ToValidationResult<T>(
        this IValidator<T> validator, T instance)
    {
      var fvResult = validator.Validate(instance);

      if (fvResult.IsValid)
        return ValidationResult<T>.Ok(instance);

      var errors = fvResult.Errors
          .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage)
              .WithMetadata("attemptedValue", f.AttemptedValue ?? "null"))
          .ToArray();

      return ValidationResult<T>.Fail(errors);
    }

    // ── Async ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates <paramref name="instance"/> asynchronously and returns a <see cref="Result{T}"/> directly.
    /// On success, the result wraps the original instance.
    /// On failure, errors are chained as causes on the first error.
    /// </summary>
    /// <typeparam name="T">The type of the instance being validated.</typeparam>
    /// <param name="validator">The FluentValidation validator to use.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="ct">An optional cancellation token.</param>
    public static async Task<Result<T>> ValidateToResultAsync<T>(
        this IValidator<T> validator, T instance,
        CancellationToken ct = default)
    {
      var fvResult = await validator.ValidateAsync(instance, ct);
      return fvResult.ToResult(instance);
    }

    /// <summary>
    /// Validates <paramref name="instance"/> asynchronously and returns a <see cref="ValidationResult{T}"/>
    /// containing all validation errors at once.
    /// </summary>
    /// <typeparam name="T">The type of the instance being validated.</typeparam>
    /// <param name="validator">The FluentValidation validator to use.</param>
    /// <param name="instance">The instance to validate.</param>
    /// <param name="ct">An optional cancellation token.</param>
    public static async Task<ValidationResult<T>> ToValidationResultAsync<T>(
        this IValidator<T> validator, T instance,
        CancellationToken ct = default)
    {
      var fvResult = await validator.ValidateAsync(instance, ct);

      if (fvResult.IsValid)
        return ValidationResult<T>.Ok(instance);

      var errors = fvResult.Errors
          .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
          .ToArray();

      return ValidationResult<T>.Fail(errors);
    }
  }
}