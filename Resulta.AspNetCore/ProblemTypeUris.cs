namespace Resulta.AspNetCore
{
  /// <summary>
  /// Canonical <see href="https://datatracker.ietf.org/doc/html/rfc7807">RFC 7807</see>
  /// <c>type</c> URIs that Resulta uses when constructing
  /// <see cref="Microsoft.AspNetCore.Mvc.ProblemDetails"/> responses.
  /// </summary>
  /// <remarks>
  /// The URIs point at the original HTTP status-code definitions in RFC 7231 and RFC 7235,
  /// matching the defaults used by ASP.NET Core's built-in problem-details generators.
  /// </remarks>
  public static class ProblemTypeUris
  {
    /// <summary>RFC 7231, section 6.5.4 — <c>404 Not Found</c>.</summary>
    public const string NotFound = "https://tools.ietf.org/html/rfc7231#section-6.5.4";

    /// <summary>RFC 7231, section 6.5.1 — <c>400 Bad Request</c>.</summary>
    public const string BadRequest = "https://tools.ietf.org/html/rfc7231#section-6.5.1";

    /// <summary>RFC 7235, section 3.1 — <c>401 Unauthorized</c>.</summary>
    public const string Unauthorized = "https://tools.ietf.org/html/rfc7235#section-3.1";

    /// <summary>RFC 7231, section 6.5.8 — <c>409 Conflict</c>.</summary>
    public const string Conflict = "https://tools.ietf.org/html/rfc7231#section-6.5.8";

    /// <summary>RFC 7231, section 6.6.1 — <c>500 Internal Server Error</c>.</summary>
    public const string InternalServerError = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
  }
}
