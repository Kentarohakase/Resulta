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
  /// Converts FluentValidation results directly into Result types.
  /// </summary>
  public static class FluentValidationBridge
  {
    // ── Sync ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates the instance and returns a Result&lt;T&gt; directly.
    /// </summary>
    public static Result<T> ValidateToResult<T>(this IValidator<T> validator, T instance)
    {
      var validationResult = validator.Validate(instance);
      return validationResult.ToResult(instance);
    }

    /// <summary>
    /// Converts a FluentValidation ValidationResult into a Resulta Result&lt;T&gt;.
    /// </summary>
    public static Result<T> ToResult<T>(this FVResult validationResult, T value)
    {
      if (validationResult.IsValid)
        return Result<T>.Ok(value);

      var errors = validationResult.Errors
          .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage)
              .WithMetadata("attemptedValue", f.AttemptedValue ?? "null")
              .WithMetadata("severity", f.Severity.ToString()))
          .ToList();

      // Chain all errors: first error as root, rest as causes
      var root = errors[0];
      for (int i = 1; i < errors.Count; i++)
        root = root.WithCause(errors[i]);

      return Result<T>.Fail(root);
    }

    /// <summary>
    /// Converts a FluentValidation result into a ValidationResult&lt;T&gt; (with all errors as a list).
    /// </summary>
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
    /// Asynchronously validates the instance and returns a Result&lt;T&gt;.
    /// </summary>
    public static async Task<Result<T>> ValidateToResultAsync<T>(
        this IValidator<T> validator, T instance,
        CancellationToken ct = default)
    {
      var fvResult = await validator.ValidateAsync(instance, ct);
      return fvResult.ToResult(instance);
    }

    /// <summary>
    /// Asynchronously converts a FluentValidation result into a ValidationResult&lt;T&gt;.
    /// </summary>
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