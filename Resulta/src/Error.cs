namespace Resulta
{
  /// <summary>
  /// Represents a structured error with an optional error code, metadata, cause chain, and exception.
  /// </summary>
  /// <remarks>
  /// <see cref="Error"/> is immutable — all fluent builder methods return a new instance.
  /// Use the predefined factory methods such as <see cref="NotFound"/>, <see cref="Validation"/>,
  /// <see cref="Unauthorized"/>, <see cref="Conflict"/>, and <see cref="Unexpected"/> for common error types.
  /// </remarks>
  public sealed class Error
  {
    /// <summary>Gets the human-readable error message.</summary>
    public string Message { get; }

    /// <summary>
    /// Gets the optional machine-readable error code (e.g. <c>"NOT_FOUND"</c>, <c>"VALIDATION_ERROR"</c>).
    /// </summary>
    public string? Code { get; }

    /// <summary>Gets the optional exception that caused this error.</summary>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets the optional root cause of this error, forming a cause chain.
    /// Use <see cref="WithCause"/> to attach a cause.
    /// </summary>
    public Error? CausedBy { get; }

    /// <summary>
    /// Gets additional metadata attached to this error as key-value pairs.
    /// Use <see cref="WithMetadata"/> to attach metadata.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Initializes a new <see cref="Error"/> with the given message and optional properties.
    /// </summary>
    /// <param name="message">The human-readable error message. Must not be empty or whitespace.</param>
    /// <param name="code">An optional machine-readable error code.</param>
    /// <param name="exception">An optional exception that caused this error.</param>
    /// <param name="causedBy">An optional root cause error.</param>
    /// <param name="metadata">Optional additional metadata.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="message"/> is null or whitespace.</exception>
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

    /// <summary>Returns a copy of this error with the given <paramref name="code"/> applied.</summary>
    /// <param name="code">The machine-readable error code to attach.</param>
    public Error WithCode(string code)
        => new Error(Message, code, Exception, CausedBy, new Dictionary<string, object>(Metadata));

    /// <summary>Returns a copy of this error with the given <paramref name="exception"/> attached.</summary>
    /// <param name="exception">The exception that caused this error.</param>
    public Error WithException(Exception exception)
        => new Error(Message, Code, exception, CausedBy, new Dictionary<string, object>(Metadata));

    /// <summary>
    /// Returns a copy of this error with <paramref name="cause"/> set as the root cause,
    /// forming an error cause chain.
    /// </summary>
    /// <param name="cause">The underlying error that caused this one.</param>
    public Error WithCause(Error cause)
        => new Error(Message, Code, Exception, cause, new Dictionary<string, object>(Metadata));

    /// <summary>
    /// Returns a copy of this error with an additional metadata entry.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    public Error WithMetadata(string key, object value)
    {
      var meta = new Dictionary<string, object>(Metadata) { [key] = value };
      return new Error(Message, Code, Exception, CausedBy, meta);
    }

    // ── Predefined Error Factories ───────────────────────────────────────

    /// <summary>
    /// Creates a <c>NOT_FOUND</c> error for the given <paramref name="resource"/> name.
    /// </summary>
    /// <param name="resource">The name of the resource that was not found.</param>
    public static Error NotFound(string resource)
        => new Error($"'{resource}' was not found.", code: "NOT_FOUND");

    /// <summary>
    /// Creates an <c>UNAUTHORIZED</c> error with an optional <paramref name="reason"/>.
    /// </summary>
    /// <param name="reason">An optional message explaining why access was denied.</param>
    public static Error Unauthorized(string? reason = null)
        => new Error(reason ?? "You are not authorized to perform this action.", code: "UNAUTHORIZED");

    /// <summary>
    /// Creates a <c>VALIDATION_ERROR</c> for a specific <paramref name="field"/>.
    /// Attaches the field name as metadata under the key <c>"field"</c>.
    /// </summary>
    /// <param name="field">The name of the field that failed validation.</param>
    /// <param name="message">A message describing what went wrong.</param>
    public static Error Validation(string field, string message)
        => new Error($"Validation failed for '{field}': {message}", code: "VALIDATION_ERROR")
            .WithMetadata("field", field);

    /// <summary>
    /// Creates an <c>UNEXPECTED_ERROR</c> from an <see cref="Exception"/>.
    /// Attaches the exception to the error for diagnostic purposes.
    /// </summary>
    /// <param name="ex">The exception that caused the unexpected error.</param>
    public static Error Unexpected(Exception ex)
        => new Error("An unexpected error has occurred.", code: "UNEXPECTED_ERROR", exception: ex);

    /// <summary>
    /// Creates a <c>CONFLICT</c> error with the given <paramref name="message"/>.
    /// </summary>
    /// <param name="message">A message describing the conflict (e.g. a duplicate resource).</param>
    public static Error Conflict(string message)
        => new Error(message, code: "CONFLICT");

    // ── Formatting ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override string ToString()
    {
      var parts = new System.Collections.Generic.List<string> { Message };
      if (Code is not null) parts.Add($"[{Code}]");
      if (CausedBy is not null) parts.Add($"→ Caused by: {CausedBy.Message}");
      return string.Join(" ", parts);
    }

    /// <summary>
    /// Returns a detailed multi-line string representation of this error,
    /// including code, exception, cause, and all metadata entries.
    /// </summary>
    public string ToDetailedString()
    {
      var lines = new System.Collections.Generic.List<string>
            {
                $"Error:     {Message}",
                Code is not null      ? $"  Code:      {Code}"                                         : null!,
                Exception is not null ? $"  Exception: {Exception.GetType().Name}: {Exception.Message}" : null!,
                CausedBy is not null  ? $"  Caused by: {CausedBy.Message}"                             : null!,
            };

      foreach (var kv in Metadata)
        lines.Add($"  Metadata:  {kv.Key} = {kv.Value}");

      lines.RemoveAll(l => l is null);
      return string.Join(Environment.NewLine, lines);
    }
  }
}