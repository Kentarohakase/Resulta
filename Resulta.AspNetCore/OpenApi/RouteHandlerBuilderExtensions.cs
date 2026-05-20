using System;
using System.Collections.Generic;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Resulta.AspNetCore.OpenApi
{
  /// <summary>
  /// Extension methods to register the standard Resulta error responses
  /// (404, 400 validation, 401, 409, 500) on a Minimal API endpoint
  /// in one call, so OpenAPI / Swagger documentation is complete.
  /// </summary>
  public static class RouteHandlerBuilderExtensions
  {
    /// <summary>
    /// Default set of HTTP status codes that Resulta endpoints return on failure:
    /// 400 (validation), 401 (unauthorized), 404 (not found), 409 (conflict), and 500 (internal error).
    /// </summary>
    public static readonly IReadOnlyList<int> DefaultStatusCodes = new[]
    {
      StatusCodes.Status400BadRequest,
      StatusCodes.Status401Unauthorized,
      StatusCodes.Status404NotFound,
      StatusCodes.Status409Conflict,
      StatusCodes.Status500InternalServerError
    };

    /// <summary>
    /// Adds OpenAPI/Swagger response metadata for every status code Resulta may return on failure.
    /// Equivalent to chaining the matching <c>.Produces&lt;ProblemDetails&gt;(...)</c> /
    /// <c>.Produces&lt;HttpValidationProblemDetails&gt;(400)</c> calls.
    /// </summary>
    /// <param name="builder">The Minimal API route handler builder.</param>
    public static RouteHandlerBuilder ProducesResultaErrors(this RouteHandlerBuilder builder)
        => ProducesResultaErrors(builder, statusCodes: null);

    /// <summary>
    /// Adds OpenAPI/Swagger response metadata for the given subset of status codes.
    /// </summary>
    /// <param name="builder">The Minimal API route handler builder.</param>
    /// <param name="statusCodes">
    /// The HTTP status codes to annotate. Must be a subset of <see cref="DefaultStatusCodes"/>.
    /// When <c>null</c> or empty, all default status codes are annotated.
    /// </param>
    public static RouteHandlerBuilder ProducesResultaErrors(this RouteHandlerBuilder builder, params int[]? statusCodes)
    {
      ArgumentNullException.ThrowIfNull(builder);

      var codes = (statusCodes is null || statusCodes.Length == 0) ? DefaultStatusCodes : statusCodes;
      foreach (var code in codes)
      {
        if (code == StatusCodes.Status400BadRequest)
          builder.Produces<HttpValidationProblemDetails>(code, contentType: "application/problem+json");
        else
          builder.Produces<ProblemDetails>(code, contentType: "application/problem+json");
      }
      return builder;
    }
  }
}
