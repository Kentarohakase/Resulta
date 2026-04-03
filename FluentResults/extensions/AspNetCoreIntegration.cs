using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FluentResults.Extensions.AspNetCore
{
    // ── Error Response ────────────────────────────────────────────────────────

    /// <summary>Standardized error response body returned by the API.</summary>
    public record ErrorResponse(string Message, string? Code, string? Field = null);

    // ── Result → IActionResult Converter ─────────────────────────────────────

    /// <summary>
    /// Extension methods to convert a Result into the appropriate HTTP response.
    /// </summary>
    public static class ResultHttpExtensions
    {
        /// <summary>Converts a Result&lt;T&gt; to an IActionResult with automatic status code mapping.</summary>
        public static IActionResult ToActionResult<T>(this Result<T> result, ControllerBase controller)
            => result.Match<IActionResult>(
                onSuccess: value => controller.Ok(value),
                onFailure: err   => err.Code switch
                {
                    "NOT_FOUND"        => controller.NotFound(new ErrorResponse(err.Message, err.Code)),
                    "VALIDATION_ERROR" => controller.BadRequest(new ErrorResponse(err.Message, err.Code,
                                            err.Metadata.TryGetValue("field", out var f) ? f?.ToString() : null)),
                    "UNAUTHORIZED"     => controller.Unauthorized(new ErrorResponse(err.Message, err.Code)),
                    "CONFLICT"         => controller.Conflict(new ErrorResponse(err.Message, err.Code)),
                    _                  => controller.StatusCode(500, new ErrorResponse("An internal error occurred.", "INTERNAL_ERROR"))
                }
            );

        /// <summary>Converts a non-generic Result to an IActionResult (204 No Content on success).</summary>
        public static IActionResult ToActionResult(this Result result, ControllerBase controller)
            => result.Match<IActionResult>(
                onSuccess: () => controller.NoContent(),
                onFailure: err => err.Code switch
                {
                    "NOT_FOUND"        => controller.NotFound(new ErrorResponse(err.Message, err.Code)),
                    "VALIDATION_ERROR" => controller.BadRequest(new ErrorResponse(err.Message, err.Code)),
                    "UNAUTHORIZED"     => controller.Unauthorized(new ErrorResponse(err.Message, err.Code)),
                    _                  => controller.StatusCode(500, new ErrorResponse("An internal error occurred.", "INTERNAL_ERROR"))
                }
            );
    }

    // ── Global Exception Middleware ───────────────────────────────────────────

    /// <summary>
    /// Middleware that converts unhandled exceptions into structured JSON error responses.
    /// Register with app.UseFluentResults() in Program.cs.
    /// </summary>
    public class ResultMiddleware
    {
        private readonly RequestDelegate _next;

        public ResultMiddleware(RequestDelegate next) => _next = next;

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

    // ── Minimal API Extensions ────────────────────────────────────────────────

    /// <summary>Extension methods for Minimal API endpoints.</summary>
    public static class MinimalApiExtensions
    {
        /// <summary>Converts a Result&lt;T&gt; to an IResult for use in Minimal API endpoints.</summary>
        public static IResult ToMinimalApiResult<T>(this Result<T> result)
            => result.Match(
                onSuccess: value => Results.Ok(value),
                onFailure: err   => err.Code switch
                {
                    "NOT_FOUND"        => Results.NotFound(new ErrorResponse(err.Message, err.Code)),
                    "VALIDATION_ERROR" => Results.BadRequest(new ErrorResponse(err.Message, err.Code)),
                    "UNAUTHORIZED"     => Results.Unauthorized(),
                    "CONFLICT"         => Results.Conflict(new ErrorResponse(err.Message, err.Code)),
                    _                  => Results.Problem(err.Message)
                }
            );
    }

    // ── Dependency Injection ──────────────────────────────────────────────────

    /// <summary>Extension methods for registering FluentResults with ASP.NET Core DI.</summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>Registers FluentResults services.</summary>
        public static IServiceCollection AddFluentResults(this IServiceCollection services)
            => services;

        /// <summary>Registers the global exception-handling middleware.</summary>
        public static IApplicationBuilder UseFluentResults(this IApplicationBuilder app)
        {
            app.UseMiddleware<ResultMiddleware>();
            return app;
        }
    }

    // ── Usage Example ─────────────────────────────────────────────────────────
    /*
    // Program.cs
    builder.Services.AddFluentResults();
    app.UseFluentResults(); // Global exception handling

    // Controller – no try/catch needed! ✅
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        [HttpGet("{id}")]
        public IActionResult Get(int id)
            => _service.GetUser(id).ToActionResult(this);

        [HttpPost]
        public IActionResult Create(CreateUserDto dto)
            => _service.CreateUser(dto).ToActionResult(this);

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
            => _service.DeleteUser(id).ToActionResult(this);
    }

    // Minimal API
    app.MapGet("/api/users/{id}", (int id, UserService svc)
        => svc.GetUser(id).ToMinimalApiResult());
    */
}
