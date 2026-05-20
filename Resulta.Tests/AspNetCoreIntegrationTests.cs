using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Resulta;
using Resulta.AspNetCore;

using Xunit;

namespace Resulta.Tests;

public sealed class ResultHttpExtensionsTests
{
  private readonly ControllerBase _controller;

  public ResultHttpExtensionsTests()
  {
    var services = new ServiceCollection()
        .AddMvc()
        .Services
        .BuildServiceProvider();

    _controller = new TestController
    {
      ControllerContext = new ControllerContext
      {
        HttpContext = new DefaultHttpContext
        {
          RequestServices = services
        }
      }
    };
  }

  private static ProblemDetails ProblemFrom(IActionResult actionResult)
  {
    var obj = Assert.IsType<ObjectResult>(actionResult);
    var problem = Assert.IsAssignableFrom<ProblemDetails>(obj.Value);
    Assert.Equal(obj.StatusCode, problem.Status);
    Assert.Contains("application/problem+json", obj.ContentTypes);
    return problem;
  }

  private static string? CodeExtension(ProblemDetails problem) =>
      problem.Extensions.TryGetValue(ResultProblemDetailsFactory.CodeExtensionKey, out var v) ? v?.ToString() : null;

  // ── Result<T> ────────────────────────────────────────────────────────

  [Fact]
  public void ToActionResult_Should_Return_200_When_Result_Is_Ok()
  {
    var result = Result.Ok(42);

    var actionResult = result.ToActionResult(_controller);

    var ok = Assert.IsType<OkObjectResult>(actionResult);
    Assert.Equal(42, ok.Value);
  }

  [Fact]
  public void ToActionResult_Should_Return_404_ProblemDetails_For_NOT_FOUND()
  {
    var result = Result.Fail<int>(Error.NotFound("User"));

    var problem = ProblemFrom(result.ToActionResult(_controller));

    Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    Assert.Equal("Not Found", problem.Title);
    Assert.Equal(ProblemTypeUris.NotFound, problem.Type);
    Assert.Contains("User", problem.Detail);
    Assert.Equal("NOT_FOUND", CodeExtension(problem));
  }

  [Fact]
  public void ToActionResult_Should_Return_400_HttpValidationProblemDetails_For_VALIDATION_ERROR()
  {
    var result = Result.Fail<int>(Error.Validation("name", "Name ist erforderlich"));

    var actionResult = result.ToActionResult(_controller);

    var obj = Assert.IsType<ObjectResult>(actionResult);
    Assert.Equal(StatusCodes.Status400BadRequest, obj.StatusCode);
    var validation = Assert.IsType<HttpValidationProblemDetails>(obj.Value);
    Assert.Equal("VALIDATION_ERROR", CodeExtension(validation));
    Assert.True(validation.Errors.ContainsKey("name"));
    Assert.Contains("Name ist erforderlich", validation.Errors["name"][0]);
  }

  [Fact]
  public void ToActionResult_Validation_Should_Carry_Field_In_Errors_Dictionary()
  {
    var result = Result.Fail<int>(Error.Validation("email", "Ungültige E-Mail"));

    var actionResult = result.ToActionResult(_controller);

    var obj = Assert.IsType<ObjectResult>(actionResult);
    var validation = Assert.IsType<HttpValidationProblemDetails>(obj.Value);
    Assert.True(validation.Errors.ContainsKey("email"));
  }

  [Fact]
  public void ToActionResult_Should_Return_401_ProblemDetails_For_UNAUTHORIZED()
  {
    var result = Result.Fail<int>(Error.Unauthorized("Kein Zugriff"));

    var problem = ProblemFrom(result.ToActionResult(_controller));

    Assert.Equal(StatusCodes.Status401Unauthorized, problem.Status);
    Assert.Equal("Unauthorized", problem.Title);
    Assert.Equal("UNAUTHORIZED", CodeExtension(problem));
  }

  [Fact]
  public void ToActionResult_Should_Return_409_ProblemDetails_For_CONFLICT()
  {
    var result = Result.Fail<int>(Error.Conflict("Bereits vorhanden"));

    var problem = ProblemFrom(result.ToActionResult(_controller));

    Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
    Assert.Equal("Conflict", problem.Title);
    Assert.Equal("CONFLICT", CodeExtension(problem));
  }

  [Fact]
  public void ToActionResult_Should_Return_500_With_Generic_Code_For_Unknown_Error_Code()
  {
    var result = Result.Fail<int>(new Error("Unbekannter Fehler").WithCode("SOME_UNKNOWN_CODE"));

    var problem = ProblemFrom(result.ToActionResult(_controller));

    Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
    Assert.Equal("Internal Server Error", problem.Title);
    Assert.Equal("An internal error occurred.", problem.Detail);
    Assert.Equal("INTERNAL_ERROR", CodeExtension(problem));
  }

  [Fact]
  public void ToActionResult_Should_Not_Leak_Internal_Error_Detail_For_Unknown_Code()
  {
    var result = Result.Fail<int>(new Error("Internal: connection string broken").WithCode("DB_FAIL"));

    var problem = ProblemFrom(result.ToActionResult(_controller));

    Assert.DoesNotContain("connection string", problem.Detail);
    Assert.DoesNotContain("DB_FAIL", CodeExtension(problem) ?? "");
  }

  // ── Result (non-generic) ─────────────────────────────────────────────

  [Fact]
  public void ToActionResult_NonGeneric_Should_Return_204_When_Result_Is_Ok()
  {
    var result = Result.Ok();

    var actionResult = result.ToActionResult(_controller);

    Assert.IsType<NoContentResult>(actionResult);
  }

  [Fact]
  public void ToActionResult_NonGeneric_Should_Return_404_ProblemDetails_For_NOT_FOUND()
  {
    var result = Result.Fail(Error.NotFound("Produkt"));

    var problem = ProblemFrom(result.ToActionResult(_controller));

    Assert.Equal(StatusCodes.Status404NotFound, problem.Status);
    Assert.Equal("NOT_FOUND", CodeExtension(problem));
  }

  [Fact]
  public void ToActionResult_NonGeneric_Validation_Should_Carry_Field()
  {
    var result = Result.Fail(Error.Validation("email", "Ungültig"));

    var actionResult = result.ToActionResult(_controller);

    var obj = Assert.IsType<ObjectResult>(actionResult);
    var validation = Assert.IsType<HttpValidationProblemDetails>(obj.Value);
    Assert.True(validation.Errors.ContainsKey("email"));
  }

  [Fact]
  public void ToActionResult_NonGeneric_Should_Return_401_For_UNAUTHORIZED()
  {
    var result = Result.Fail(Error.Unauthorized("Nicht erlaubt"));

    var problem = ProblemFrom(result.ToActionResult(_controller));

    Assert.Equal(StatusCodes.Status401Unauthorized, problem.Status);
    Assert.Equal("UNAUTHORIZED", CodeExtension(problem));
  }

  [Fact]
  public void ToActionResult_NonGeneric_Should_Return_409_For_CONFLICT()
  {
    var result = Result.Fail(Error.Conflict("Duplikat"));

    var problem = ProblemFrom(result.ToActionResult(_controller));

    Assert.Equal(StatusCodes.Status409Conflict, problem.Status);
    Assert.Equal("CONFLICT", CodeExtension(problem));
  }

  [Fact]
  public void ToActionResult_NonGeneric_Should_Return_500_For_Unknown_Code()
  {
    var result = Result.Fail(new Error("Unbekannt").WithCode("UNKNOWN"));

    var problem = ProblemFrom(result.ToActionResult(_controller));

    Assert.Equal(StatusCodes.Status500InternalServerError, problem.Status);
    Assert.Equal("INTERNAL_ERROR", CodeExtension(problem));
  }
}

public sealed class MinimalApiExtensionsTests
{
  private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

  private static IServiceProvider CreateMinimalApiServices()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddOptions();
    services.AddSingleton<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>(Options.Create(new Microsoft.AspNetCore.Http.Json.JsonOptions()));
    return services.BuildServiceProvider();
  }

  private static async Task<(int StatusCode, string? ContentType, JsonElement? Body)> ExecuteAsync(IResult result)
  {
    var ctx = new DefaultHttpContext
    {
      RequestServices = CreateMinimalApiServices()
    };
    ctx.Response.Body = new MemoryStream();

    await result.ExecuteAsync(ctx);

    ctx.Response.Body.Seek(0, SeekOrigin.Begin);
    var raw = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
    JsonElement? body = string.IsNullOrWhiteSpace(raw) ? null : JsonDocument.Parse(raw).RootElement;
    return (ctx.Response.StatusCode, ctx.Response.ContentType, body);
  }

  [Fact]
  public async Task ToMinimalApiResult_Should_Return_200_On_Success()
  {
    var apiResult = Result.Ok(42).ToMinimalApiResult();

    var (status, _, _) = await ExecuteAsync(apiResult);

    Assert.Equal(StatusCodes.Status200OK, status);
  }

  [Fact]
  public async Task ToMinimalApiResult_NonGeneric_Should_Return_204_On_Success()
  {
    var apiResult = Result.Ok().ToMinimalApiResult();

    var (status, _, _) = await ExecuteAsync(apiResult);

    Assert.Equal(StatusCodes.Status204NoContent, status);
  }

  [Fact]
  public async Task ToMinimalApiResult_Should_Return_ProblemDetails_For_NOT_FOUND()
  {
    var apiResult = Result.Fail<int>(Error.NotFound("User")).ToMinimalApiResult();

    var (status, contentType, body) = await ExecuteAsync(apiResult);

    Assert.Equal(StatusCodes.Status404NotFound, status);
    Assert.StartsWith("application/problem+json", contentType);
    Assert.NotNull(body);
    Assert.Equal(404, body!.Value.GetProperty("status").GetInt32());
    Assert.Equal("Not Found", body.Value.GetProperty("title").GetString());
    Assert.Equal("NOT_FOUND", body.Value.GetProperty("code").GetString());
  }

  [Fact]
  public async Task ToMinimalApiResult_Should_Include_Errors_Dict_For_VALIDATION_ERROR()
  {
    var apiResult = Result.Fail<int>(Error.Validation("email", "Ungültig")).ToMinimalApiResult();

    var (status, contentType, body) = await ExecuteAsync(apiResult);

    Assert.Equal(StatusCodes.Status400BadRequest, status);
    Assert.StartsWith("application/problem+json", contentType);
    Assert.NotNull(body);
    Assert.Equal("VALIDATION_ERROR", body!.Value.GetProperty("code").GetString());
    var errors = body.Value.GetProperty("errors");
    Assert.True(errors.TryGetProperty("email", out _));
  }

  [Fact]
  public async Task ToMinimalApiResult_Should_Return_401_For_UNAUTHORIZED()
  {
    var apiResult = Result.Fail<int>(Error.Unauthorized("Kein Zugriff")).ToMinimalApiResult();

    var (status, _, body) = await ExecuteAsync(apiResult);

    Assert.Equal(StatusCodes.Status401Unauthorized, status);
    Assert.Equal("UNAUTHORIZED", body!.Value.GetProperty("code").GetString());
  }

  [Fact]
  public async Task ToMinimalApiResult_Should_Return_409_For_CONFLICT()
  {
    var apiResult = Result.Fail<int>(Error.Conflict("Bereits vorhanden")).ToMinimalApiResult();

    var (status, _, body) = await ExecuteAsync(apiResult);

    Assert.Equal(StatusCodes.Status409Conflict, status);
    Assert.Equal("CONFLICT", body!.Value.GetProperty("code").GetString());
  }

  [Fact]
  public async Task ToMinimalApiResult_Should_Return_500_With_Generic_Code_For_Unknown()
  {
    var apiResult = Result.Fail<int>(new Error("Unbekannt").WithCode("UNKNOWN")).ToMinimalApiResult();

    var (status, _, body) = await ExecuteAsync(apiResult);

    Assert.Equal(StatusCodes.Status500InternalServerError, status);
    Assert.Equal("INTERNAL_ERROR", body!.Value.GetProperty("code").GetString());
    Assert.Equal("An internal error occurred.", body.Value.GetProperty("detail").GetString());
  }
}

public sealed class ResultMiddlewareTests
{
  [Fact]
  public async Task Middleware_Should_Pass_Through_When_No_Exception_Is_Thrown()
  {
    var host = await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
          webBuilder.UseTestServer();
          webBuilder.Configure(app =>
          {
            app.UseMiddleware<ResultMiddleware>();
            app.Run(ctx => ctx.Response.WriteAsync("OK"));
          });
        })
        .StartAsync();

    var client = host.GetTestClient();
    var response = await client.GetAsync("/");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var body = await response.Content.ReadAsStringAsync();
    Assert.Equal("OK", body);
  }

  [Fact]
  public async Task Middleware_Should_Return_500_ProblemDetails_When_Unhandled_Exception_Is_Thrown()
  {
    var host = await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
          webBuilder.UseTestServer();
          webBuilder.Configure(app =>
          {
            app.UseMiddleware<ResultMiddleware>();
            app.Run(_ => throw new InvalidOperationException("Unbehandelt"));
          });
        })
        .StartAsync();

    var client = host.GetTestClient();
    var response = await client.GetAsync("/");

    Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

    var raw = await response.Content.ReadAsStringAsync();
    var body = JsonDocument.Parse(raw).RootElement;
    Assert.Equal(500, body.GetProperty("status").GetInt32());
    Assert.Equal("Internal Server Error", body.GetProperty("title").GetString());
    Assert.Equal("INTERNAL_ERROR", body.GetProperty("code").GetString());
  }

  [Fact]
  public async Task Middleware_Should_Not_Leak_Exception_Message()
  {
    var host = await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
          webBuilder.UseTestServer();
          webBuilder.Configure(app =>
          {
            app.UseMiddleware<ResultMiddleware>();
            app.Run(_ => throw new InvalidOperationException("Secret connection string: Server=prod;Password=swordfish"));
          });
        })
        .StartAsync();

    var client = host.GetTestClient();
    var response = await client.GetAsync("/");
    var body = await response.Content.ReadAsStringAsync();

    Assert.DoesNotContain("swordfish", body);
    Assert.DoesNotContain("connection string", body);
  }
}

public sealed class TypedMinimalApiExtensionsTests
{
  private static IServiceProvider CreateServices()
  {
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddOptions();
    services.AddSingleton<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>(Options.Create(new Microsoft.AspNetCore.Http.Json.JsonOptions()));
    return services.BuildServiceProvider();
  }

  private static async Task<(int Status, string? ContentType, JsonElement? Body)> ExecuteAsync(IResult result)
  {
    var ctx = new DefaultHttpContext { RequestServices = CreateServices() };
    ctx.Response.Body = new MemoryStream();
    await result.ExecuteAsync(ctx);
    ctx.Response.Body.Seek(0, SeekOrigin.Begin);
    var raw = await new StreamReader(ctx.Response.Body).ReadToEndAsync();
    JsonElement? body = string.IsNullOrWhiteSpace(raw) ? null : JsonDocument.Parse(raw).RootElement;
    return (ctx.Response.StatusCode, ctx.Response.ContentType, body);
  }

  [Fact]
  public async Task ToTypedResult_Ok_Should_Return_200_With_Value()
  {
    var typed = Result.Ok(42).ToTypedResult();

    var (status, _, body) = await ExecuteAsync(typed);

    Assert.Equal(StatusCodes.Status200OK, status);
    Assert.Equal(42, body!.Value.GetInt32());
  }

  [Fact]
  public async Task ToTypedResult_NonGeneric_Ok_Should_Return_204()
  {
    var typed = Result.Ok().ToTypedResult();

    var (status, _, _) = await ExecuteAsync(typed);

    Assert.Equal(StatusCodes.Status204NoContent, status);
  }

  [Fact]
  public async Task ToTypedResult_Should_Return_404_For_NOT_FOUND()
  {
    var typed = Result.Fail<int>(Error.NotFound("User")).ToTypedResult();

    var (status, _, body) = await ExecuteAsync(typed);

    Assert.Equal(StatusCodes.Status404NotFound, status);
    Assert.Equal("NOT_FOUND", body!.Value.GetProperty("code").GetString());
  }

  [Fact]
  public async Task ToTypedResult_Should_Return_400_Validation_Problem()
  {
    var typed = Result.Fail<int>(Error.Validation("email", "Ungültig")).ToTypedResult();

    var (status, _, body) = await ExecuteAsync(typed);

    Assert.Equal(StatusCodes.Status400BadRequest, status);
    Assert.True(body!.Value.GetProperty("errors").TryGetProperty("email", out _));
  }

  [Fact]
  public async Task ToTypedResult_Should_Return_409_For_CONFLICT()
  {
    var typed = Result.Fail<int>(Error.Conflict("Duplikat")).ToTypedResult();

    var (status, _, body) = await ExecuteAsync(typed);

    Assert.Equal(StatusCodes.Status409Conflict, status);
    Assert.Equal("CONFLICT", body!.Value.GetProperty("code").GetString());
  }

  [Fact]
  public async Task ToTypedResult_Should_Return_401_For_UNAUTHORIZED_Via_Generic_Problem()
  {
    var typed = Result.Fail<int>(Error.Unauthorized("Kein Zugriff")).ToTypedResult();

    var (status, _, body) = await ExecuteAsync(typed);

    Assert.Equal(StatusCodes.Status401Unauthorized, status);
    Assert.Equal("UNAUTHORIZED", body!.Value.GetProperty("code").GetString());
  }

  [Fact]
  public async Task ToTypedResult_Should_Return_500_Generic_For_Unknown()
  {
    var typed = Result.Fail<int>(new Error("Unbekannt").WithCode("UNKNOWN")).ToTypedResult();

    var (status, _, body) = await ExecuteAsync(typed);

    Assert.Equal(StatusCodes.Status500InternalServerError, status);
    Assert.Equal("INTERNAL_ERROR", body!.Value.GetProperty("code").GetString());
  }
}

internal sealed class TestController : ControllerBase { }
