using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Resulta;

namespace Resulta.AspNetCore
{
  internal static class ResultHttpMapping
  {
    /// <summary>Optional <c>field</c> from <see cref="Error.Metadata"/> for <c>VALIDATION_ERROR</c> responses.</summary>
    internal static string? ValidationFieldFromMetadata(Error err) =>
        err.Metadata.TryGetValue("field", out var v) ? v?.ToString() : null;
  }

  /// <summary>
  /// Standardized JSON error response body returned by the API on failure.
  /// </summary>
  /// <param name="Message">A human-readable description of the error.</param>
  /// <param name="Code">A machine-readable error code (e.g. <c>"NOT_FOUND"</c>).</param>
  /// <param name="Field">An optional field name for validation errors.</param>
  public record ErrorResponse(string Message, string? Code, string? Field = null);

  /// <summary>
  /// Extension methods to convert a <see cref="Result"/> or <see cref="Result{T}"/>
  /// into the appropriate HTTP response for MVC controllers and Minimal API endpoints.
  /// </summary>
  public static class ResultHttpExtensions
  {
    /// <summary>
    /// Converts a <see cref="Result{T}"/> to an <see cref="IActionResult"/> with automatic HTTP status code mapping.
    /// Returns <c>200 OK</c> with the value on success, or an error response on failure.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="controller">The controller used to create action results.</param>
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
        => result.Match<IActionResult>(
            onSuccess: value => controller.Ok(value),
            onFailure: err => err.Code switch
            {
              "NOT_FOUND" => controller.NotFound(new ErrorResponse(err.Message, err.Code)),
              "VALIDATION_ERROR" => controller.BadRequest(new ErrorResponse(err.Message, err.Code,
                                          ResultHttpMapping.ValidationFieldFromMetadata(err))),
              "UNAUTHORIZED" => controller.Unauthorized(new ErrorResponse(err.Message, err.Code)),
              "CONFLICT" => controller.Conflict(new ErrorResponse(err.Message, err.Code)),
              _ => controller.StatusCode(500, new ErrorResponse("An internal error occurred.", "INTERNAL_ERROR"))
            }
        );

    /// <summary>
    /// Converts a non-generic <see cref="Result"/> to an <see cref="IActionResult"/>.
    /// Returns <c>204 No Content</c> on success, or an error response on failure.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="controller">The controller used to create action results.</param>
    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
        => result.Match<IActionResult>(
            onSuccess: () => controller.NoContent(),
            onFailure: err => err.Code switch
            {
              "NOT_FOUND" => controller.NotFound(new ErrorResponse(err.Message, err.Code)),
              "VALIDATION_ERROR" => controller.BadRequest(new ErrorResponse(err.Message, err.Code,
                                          ResultHttpMapping.ValidationFieldFromMetadata(err))),
              "UNAUTHORIZED" => controller.Unauthorized(new ErrorResponse(err.Message, err.Code)),
              "CONFLICT" => controller.Conflict(new ErrorResponse(err.Message, err.Code)),
              _ => controller.StatusCode(500, new ErrorResponse("An internal error occurred.", "INTERNAL_ERROR"))
            }
        );
  }

  /// <summary>
  /// Middleware that catches unhandled exceptions and converts them into structured JSON error responses
  /// with HTTP status code <c>500 Internal Server Error</c>.
  /// Register with <c>app.UseResulta()</c> in <c>Program.cs</c>.
  /// </summary>
  public class ResultMiddleware
  {
    private readonly RequestDelegate _next;

    /// <summary>Initializes a new instance of <see cref="ResultMiddleware"/>.</summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public ResultMiddleware(RequestDelegate next) => _next = next;

    /// <summary>Invokes the middleware, catching any unhandled exceptions.</summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
      try
      {
        await _next(context);
      }
      catch (Exception ex)
      {
        await HandleExceptionAsync(context, ex);
      }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
      var error = Error.Unexpected(ex);
      var response = new ErrorResponse("An unexpected error has occurred.", error.Code);
      context.Response.ContentType = "application/json";
      context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
      return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
  }

  /// <summary>
  /// Extension methods for converting <see cref="Result{T}"/> into <see cref="IResult"/>
  /// for use in Minimal API endpoints.
  /// </summary>
  public static class MinimalApiExtensions
  {
    /// <summary>
    /// Converts a <see cref="Result{T}"/> to an <see cref="IResult"/> for Minimal API endpoints,
    /// with automatic HTTP status code mapping based on the error code.
    /// Returns <c>200 OK</c> on success, or an appropriate error response on failure.
    /// </summary>
    /// <remarks>
    /// Mapping matches <see cref="ResultHttpExtensions.ToActionResult{T}(Result{T}, ControllerBase)"/>:
    /// <c>VALIDATION_ERROR</c> includes optional <c>field</c> from <see cref="Error.Metadata"/>;
    /// <c>UNAUTHORIZED</c> returns <c>401</c> with a JSON <see cref="ErrorResponse"/> body (not an empty response);
    /// unknown error codes yield <c>500</c> with code <c>INTERNAL_ERROR</c>, same as MVC.
    /// </remarks>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to convert.</param>
    public static IResult ToMinimalApiResult<T>(this Result<T> result)
        => result.Match(
            onSuccess: value => Results.Ok(value),
            onFailure: err => err.Code switch
            {
              "NOT_FOUND" => Results.NotFound(new ErrorResponse(err.Message, err.Code)),
              "VALIDATION_ERROR" => Results.BadRequest(new ErrorResponse(err.Message, err.Code,
                                          ResultHttpMapping.ValidationFieldFromMetadata(err))),
              "UNAUTHORIZED" => Results.Json(
                  new ErrorResponse(err.Message, err.Code),
                  statusCode: StatusCodes.Status401Unauthorized),
              "CONFLICT" => Results.Conflict(new ErrorResponse(err.Message, err.Code)),
              _ => Results.Json(
                  new ErrorResponse("An internal error occurred.", "INTERNAL_ERROR"),
                  statusCode: StatusCodes.Status500InternalServerError)
            }
        );
  }

  /// <summary>
  /// Extension methods for registering Resulta services and middleware with ASP.NET Core.
  /// </summary>
  public static class ServiceCollectionExtensions
  {
    /// <summary>
    /// Registers Resulta services with the dependency injection container.
    /// Call this in <c>Program.cs</c> before <c>app.Build()</c>.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    public static IServiceCollection AddResulta(this IServiceCollection services)
        => services;

    /// <summary>
    /// Registers the <see cref="ResultMiddleware"/> for global unhandled exception handling.
    /// Call this in <c>Program.cs</c> after <c>app.Build()</c>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    public static IApplicationBuilder UseResulta(this IApplicationBuilder app)
    {
      app.UseMiddleware<ResultMiddleware>();
      return app;
    }
  }
}