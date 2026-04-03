namespace Resulta
{
    /// <summary>
    /// Represents a structured error with an optional code, metadata, and cause chain.
    /// </summary>
    public sealed class Error
    {
        /// <summary>
        /// Human-readable error message describing what went wrong.
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Optional machine-readable error code.
        /// </summary>
        public string? Code { get; }

        /// <summary>
        /// Optional exception associated with this error.
        /// </summary>
        public Exception? Exception { get; }

        /// <summary>
        /// Optional causal error that caused this error.
        /// </summary>
        public Error? CausedBy { get; }

        /// <summary>
        /// Additional metadata associated with this error.
        /// </summary>
        public IReadOnlyDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class with the specified details.
        /// </summary>
        public Error(string message, string? code = null, Exception? exception = null,
                     Error? causedBy = null, Dictionary<string, object>? metadata = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Error message must not be empty.", nameof(message));

            Message = message;
            Code = code;
            Exception = exception;
            CausedBy = causedBy;
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        // ── Fluent Builder ───────────────────────────────────────────────────

        /// <summary>Returns a copy of this error with the given code.</summary>
        public Error WithCode(string code)
            => new Error(Message, code, Exception, CausedBy, new Dictionary<string, object>(Metadata));

        /// <summary>Returns a copy of this error with the given exception attached.</summary>
        public Error WithException(Exception exception)
            => new Error(Message, Code, exception, CausedBy, new Dictionary<string, object>(Metadata));

        /// <summary>Returns a copy of this error with a root cause attached.</summary>
        public Error WithCause(Error cause)
            => new Error(Message, Code, Exception, cause, new Dictionary<string, object>(Metadata));

        /// <summary>Returns a copy of this error with an additional metadata entry.</summary>
        public Error WithMetadata(string key, object value)
        {
            var meta = new Dictionary<string, object>(Metadata) { [key] = value };
            return new Error(Message, Code, Exception, CausedBy, meta);
        }

        // ── Predefined Error Factories ───────────────────────────────────────

        /// <summary>Creates a NOT_FOUND error for the given resource name.</summary>
        public static Error NotFound(string resource)
            => new Error($"'{resource}' was not found.", code: "NOT_FOUND");

        /// <summary>Creates an UNAUTHORIZED error with an optional reason.</summary>
        public static Error Unauthorized(string? reason = null)
            => new Error(reason ?? "You are not authorized to perform this action.", code: "UNAUTHORIZED");

        /// <summary>Creates a VALIDATION_ERROR for a specific field.</summary>
        public static Error Validation(string field, string message)
            => new Error($"Validation failed for '{field}': {message}", code: "VALIDATION_ERROR")
                .WithMetadata("field", field);

        /// <summary>Creates an UNEXPECTED_ERROR from an exception.</summary>
        public static Error Unexpected(Exception ex)
            => new Error("An unexpected error has occurred.", code: "UNEXPECTED_ERROR", exception: ex);

        /// <summary>Creates a CONFLICT error with the given message.</summary>
        public static Error Conflict(string message)
            => new Error(message, code: "CONFLICT");

        // ── Formatting ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns a short string representation of the error, including message, code and immediate cause.
        /// </summary>
        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string> { Message };
            if (Code is not null) parts.Add($"[{Code}]");
            if (CausedBy is not null) parts.Add($"→ Caused by: {CausedBy.Message}");
            return string.Join(" ", parts);
        }

        /// <summary>Returns a detailed multi-line string representation of this error.</summary>
        public string ToDetailedString()
        {
            var lines = new System.Collections.Generic.List<string>
            {
                $"Error:     {Message}",
                Code is not null      ? $"  Code:      {Code}"                                    : null!,
                Exception is not null ? $"  Exception: {Exception.GetType().Name}: {Exception.Message}" : null!,
                CausedBy is not null  ? $"  Caused by: {CausedBy.Message}"                        : null!,
            };

            foreach (var kv in Metadata)
                lines.Add($"  Metadata:  {kv.Key} = {kv.Value}");

            lines.RemoveAll(l => l is null);
            return string.Join(Environment.NewLine, lines);
        }
    }
}
