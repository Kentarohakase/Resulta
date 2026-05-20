using System;
using System.Collections.Generic;
using System.Net;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

using Resulta;

namespace Resulta.AspNetCore
{
  /// <summary>
  /// Builds RFC 7807 <see cref="ProblemDetails"/> (or <see cref="HttpValidationProblemDetails"/>)
  /// responses from a Resulta <see cref="Error"/>, using the error's <see cref="Error.Code"/> to
  /// pick the HTTP status, title, and <c>type</c> URI.
  /// </summary>
  /// <remarks>
  /// The mapping is:
  /// <list type="bullet">
  ///   <item><description><c>NOT_FOUND</c> → <c>404 Not Found</c></description></item>
  ///   <item><description><c>VALIDATION_ERROR</c> → <c>400 Bad Request</c> with field/message in <see cref="HttpValidationProblemDetails.Errors"/></description></item>
  ///   <item><description><c>UNAUTHORIZED</c> → <c>401 Unauthorized</c></description></item>
  ///   <item><description><c>CONFLICT</c> → <c>409 Conflict</c></description></item>
  ///   <item><description>Any other code → <c>500 Internal Server Error</c></description></item>
  /// </list>
  /// The original error code is also attached as the <c>code</c> extension property
  /// (<see cref="ProblemDetails.Extensions"/>), so machine-readable callers can branch on it.
  /// </remarks>
  public static class ResultProblemDetailsFactory
  {
    /// <summary>JSON extension key under which the original <see cref="Error.Code"/> is exposed on the problem object.</summary>
    public const string CodeExtensionKey = "code";

    private const string ValidationFieldMetadataKey = "field";

    /// <summary>
    /// Constructs a <see cref="ProblemDetails"/> (or <see cref="HttpValidationProblemDetails"/> for
    /// validation errors) from the given <paramref name="error"/>.
    /// </summary>
    /// <param name="error">The error to map.</param>
    /// <param name="context">Optional HTTP context; when supplied, <see cref="ProblemDetails.Instance"/> is set to the request path.</param>
    /// <remarks>
    /// Errors whose <see cref="Error.Code"/> is unknown (anything outside the five recognized codes)
    /// are flattened to a generic <c>500 Internal Server Error</c> with code <c>INTERNAL_ERROR</c> and
    /// the detail <c>"An internal error occurred."</c>, so that internal error codes or exception
    /// messages do not leak to clients.
    /// </remarks>
    public static ProblemDetails Create(Error error, HttpContext? context = null)
    {
      ArgumentNullException.ThrowIfNull(error);

      if (error.Code is null || !IsKnownCode(error.Code))
        return BuildGenericInternalError(context);

      var problem = error.Code == "VALIDATION_ERROR"
          ? BuildValidationProblem(error)
          : new ProblemDetails();

      problem.Status = StatusFor(error.Code);
      problem.Title = TitleFor(error.Code);
      problem.Type = TypeFor(error.Code);
      problem.Detail = error.Message;
      problem.Instance = context?.Request.Path.Value;
      problem.Extensions[CodeExtensionKey] = error.Code;

      return problem;
    }

    private static ProblemDetails BuildGenericInternalError(HttpContext? context)
    {
      var generic = new ProblemDetails
      {
        Status = StatusCodes.Status500InternalServerError,
        Title = "Internal Server Error",
        Type = ProblemTypeUris.InternalServerError,
        Detail = "An internal error occurred.",
        Instance = context?.Request.Path.Value
      };
      generic.Extensions[CodeExtensionKey] = "INTERNAL_ERROR";
      return generic;
    }

    private static bool IsKnownCode(string code) => code is
        "NOT_FOUND" or "VALIDATION_ERROR" or "UNAUTHORIZED" or "CONFLICT";

    private static HttpValidationProblemDetails BuildValidationProblem(Error error)
    {
      var validation = new HttpValidationProblemDetails();
      if (error.Metadata.TryGetValue(ValidationFieldMetadataKey, out var v) && v?.ToString() is { Length: > 0 } field)
        validation.Errors[field] = new[] { error.Message };
      return validation;
    }

    internal static int StatusFor(string? code) => code switch
    {
      "NOT_FOUND" => StatusCodes.Status404NotFound,
      "VALIDATION_ERROR" => StatusCodes.Status400BadRequest,
      "UNAUTHORIZED" => StatusCodes.Status401Unauthorized,
      "CONFLICT" => StatusCodes.Status409Conflict,
      _ => StatusCodes.Status500InternalServerError
    };

    private static string TitleFor(string? code) => code switch
    {
      "NOT_FOUND" => "Not Found",
      "VALIDATION_ERROR" => "Validation Error",
      "UNAUTHORIZED" => "Unauthorized",
      "CONFLICT" => "Conflict",
      _ => "Internal Server Error"
    };

    private static string TypeFor(string? code) => code switch
    {
      "NOT_FOUND" => ProblemTypeUris.NotFound,
      "VALIDATION_ERROR" => ProblemTypeUris.BadRequest,
      "UNAUTHORIZED" => ProblemTypeUris.Unauthorized,
      "CONFLICT" => ProblemTypeUris.Conflict,
      _ => ProblemTypeUris.InternalServerError
    };
  }

  /// <summary>
  /// Extension methods to convert a <see cref="Result"/> or <see cref="Result{T}"/>
  /// into an <see cref="IActionResult"/> for MVC controllers, using RFC 7807
  /// <see cref="ProblemDetails"/> bodies on failure.
  /// </summary>
  public static class ResultHttpExtensions
  {
    /// <summary>
    /// Converts a <see cref="Result{T}"/> to an <see cref="IActionResult"/>.
    /// Returns <c>200 OK</c> with the value on success, or a <see cref="ProblemDetails"/>
    /// response with the appropriate HTTP status code on failure.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to convert.</param>
    /// <param name="controller">The controller used to create action results.</param>
    public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
    {
      ArgumentNullException.ThrowIfNull(result);
      ArgumentNullException.ThrowIfNull(controller);
      return result.Match<IActionResult>(
          onSuccess: value => controller.Ok(value),
          onFailure: err => ProblemResultFor(err, controller));
    }

    /// <summary>
    /// Converts a non-generic <see cref="Result"/> to an <see cref="IActionResult"/>.
    /// Returns <c>204 No Content</c> on success, or a <see cref="ProblemDetails"/>
    /// response with the appropriate HTTP status code on failure.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    /// <param name="controller">The controller used to create action results.</param>
    public static IActionResult ToActionResult(this Result result, ControllerBase controller)
    {
      ArgumentNullException.ThrowIfNull(result);
      ArgumentNullException.ThrowIfNull(controller);
      return result.Match<IActionResult>(
          onSuccess: () => controller.NoContent(),
          onFailure: err => ProblemResultFor(err, controller));
    }

    private static IActionResult ProblemResultFor(Error err, ControllerBase controller)
    {
      var problem = ResultProblemDetailsFactory.Create(err, controller.HttpContext);
      return new ObjectResult(problem)
      {
        StatusCode = problem.Status,
        ContentTypes = { "application/problem+json" }
      };
    }
  }

  /// <summary>
  /// Middleware that catches unhandled exceptions and converts them into a
  /// <c>500 Internal Server Error</c> <see cref="ProblemDetails"/> response.
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
      var problem = ResultProblemDetailsFactory.Create(error, context);
      context.Response.StatusCode = problem.Status ?? (int)HttpStatusCode.InternalServerError;
      return context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }
  }

  /// <summary>
  /// Extension methods for converting <see cref="Result"/> and <see cref="Result{T}"/>
  /// into <see cref="IResult"/> values for use in Minimal API endpoints, with RFC 7807
  /// <see cref="ProblemDetails"/> bodies on failure.
  /// </summary>
  public static class MinimalApiExtensions
  {
    /// <summary>
    /// Converts a <see cref="Result{T}"/> to an <see cref="IResult"/> for Minimal API endpoints.
    /// Returns <c>200 OK</c> with the value on success, or a <see cref="ProblemDetails"/>
    /// response with the appropriate HTTP status code on failure.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to convert.</param>
    public static IResult ToMinimalApiResult<T>(this Result<T> result)
    {
      ArgumentNullException.ThrowIfNull(result);
      return result.Match(
          onSuccess: value => Results.Ok(value),
          onFailure: err => ProblemResultFor(err));
    }

    /// <summary>
    /// Converts a non-generic <see cref="Result"/> to an <see cref="IResult"/> for Minimal API endpoints.
    /// Returns <c>204 No Content</c> on success, or a <see cref="ProblemDetails"/> response on failure.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    public static IResult ToMinimalApiResult(this Result result)
    {
      ArgumentNullException.ThrowIfNull(result);
      return result.Match(
          onSuccess: () => Results.NoContent(),
          onFailure: err => ProblemResultFor(err));
    }

    private static IResult ProblemResultFor(Error err)
    {
      var problem = ResultProblemDetailsFactory.Create(err);
      return Results.Json(problem, statusCode: problem.Status, contentType: "application/problem+json");
    }
  }

  /// <summary>
  /// Strongly-typed Minimal API extensions that return discriminated <see cref="Results{TResult1, TResult2, TResult3, TResult4, TResult5}"/>
  /// unions, so OpenAPI / Swagger can document every possible response shape for the endpoint.
  /// </summary>
  /// <remarks>
  /// Use these instead of <see cref="MinimalApiExtensions.ToMinimalApiResult{T}(Result{T})"/> when you
  /// want endpoint metadata (and therefore generated client SDKs) to reflect the full response surface
  /// of a Resulta endpoint: 200/204 success, 404, 400 validation, 409, and a generic
  /// <see cref="ProblemHttpResult"/> for 401 and 500.
  /// </remarks>
  public static class TypedMinimalApiExtensions
  {
    /// <summary>
    /// Converts a <see cref="Result{T}"/> to a typed Minimal API result union.
    /// </summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="result">The result to convert.</param>
    public static Results<Ok<T>, NotFound<ProblemDetails>, BadRequest<HttpValidationProblemDetails>, Conflict<ProblemDetails>, ProblemHttpResult> ToTypedResult<T>(this Result<T> result)
    {
      ArgumentNullException.ThrowIfNull(result);
      if (result.IsSuccess) return TypedResults.Ok(result.Value);

      var problem = ResultProblemDetailsFactory.Create(result.Error);
      if (result.Error.Code == "NOT_FOUND") return TypedResults.NotFound(problem);
      if (result.Error.Code == "VALIDATION_ERROR") return TypedResults.BadRequest((HttpValidationProblemDetails)problem);
      if (result.Error.Code == "CONFLICT") return TypedResults.Conflict(problem);
      return TypedResults.Problem(problem);
    }

    /// <summary>
    /// Converts a non-generic <see cref="Result"/> to a typed Minimal API result union.
    /// </summary>
    /// <param name="result">The result to convert.</param>
    public static Results<NoContent, NotFound<ProblemDetails>, BadRequest<HttpValidationProblemDetails>, Conflict<ProblemDetails>, ProblemHttpResult> ToTypedResult(this Result result)
    {
      ArgumentNullException.ThrowIfNull(result);
      if (result.IsSuccess) return TypedResults.NoContent();

      var problem = ResultProblemDetailsFactory.Create(result.Error);
      if (result.Error.Code == "NOT_FOUND") return TypedResults.NotFound(problem);
      if (result.Error.Code == "VALIDATION_ERROR") return TypedResults.BadRequest((HttpValidationProblemDetails)problem);
      if (result.Error.Code == "CONFLICT") return TypedResults.Conflict(problem);
      return TypedResults.Problem(problem);
    }
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
    {
      ArgumentNullException.ThrowIfNull(services);
      return services;
    }

    /// <summary>
    /// Registers the <see cref="ResultMiddleware"/> for global unhandled exception handling.
    /// Call this in <c>Program.cs</c> after <c>app.Build()</c>.
    /// </summary>
    /// <param name="app">The application builder.</param>
    public static IApplicationBuilder UseResulta(this IApplicationBuilder app)
    {
      ArgumentNullException.ThrowIfNull(app);
      app.UseMiddleware<ResultMiddleware>();
      return app;
    }
  }
}
