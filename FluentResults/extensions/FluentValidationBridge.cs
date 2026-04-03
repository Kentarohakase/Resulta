using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FluentValidation.Results;

namespace FluentResults.Extensions.FluentValidation
{
    /// <summary>
    /// Bridge between FluentValidation and FluentResults.
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
        /// Converts a FluentValidation ValidationResult into a FluentResults Result&lt;T&gt;.
        /// </summary>
        public static Result<T> ToResult<T>(this ValidationResult validationResult, T value)
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

    // ── Usage Example ─────────────────────────────────────────────────────────
    /*
    // 1. Define a FluentValidation validator as usual
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name must not be empty")
                .MinimumLength(2).WithMessage("Name must be at least 2 characters");

            RuleFor(x => x.Email)
                .NotEmpty()
                .EmailAddress().WithMessage("Must be a valid email address");

            RuleFor(x => x.Age)
                .GreaterThanOrEqualTo(18).WithMessage("Must be at least 18 years old");
        }
    }

    // 2. Use it in a service to get a Result directly
    public class UserService
    {
        private readonly IValidator<RegisterDto> _validator;

        public Result<User> Register(RegisterDto dto)
        {
            return _validator.ValidateToResult(dto)   // → Result<RegisterDto>
                .Bind(CreateUser);                     // → Result<User>
        }

        public async Task<Result<User>> RegisterAsync(RegisterDto dto)
        {
            return await _validator
                .ValidateToResultAsync(dto)            // → Task<Result<RegisterDto>>
                .Bind(CreateUserAsync);                // → Task<Result<User>>
        }
    }

    // 3. Get all errors as a list (ValidationResult)
    var result = _validator.ToValidationResult(dto);
    result.Match(
        onSuccess: dto    => Console.WriteLine("OK!"),
        onFailure: errors =>
        {
            foreach (var e in errors)
                Console.WriteLine($"  x {e.Message}");
        }
    );

    // NuGet package required: FluentValidation
    // dotnet add package FluentValidation
    */
}
