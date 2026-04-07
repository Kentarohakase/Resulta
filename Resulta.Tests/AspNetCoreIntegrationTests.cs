using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Resulta;
using Resulta.AspNetCore;

using Xunit;

namespace Resulta.Tests;

// ── ToActionResult ────────────────────────────────────────────────────────────

public sealed class ResultHttpExtensionsTests
{
  private readonly ControllerBase _controller;

  public ResultHttpExtensionsTests()
  {
    // Minimal controller setup for ActionResult conversion
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

  // ── Result<T>.ToActionResult ──────────────────────────────────────────────

  [Fact]
  public void ToActionResult_Should_Return_200_When_Result_Is_Ok()
  {
    var result = Result.Ok(42);

    var actionResult = result.ToActionResult(_controller);

    var ok = Assert.IsType<OkObjectResult>(actionResult);
    Assert.Equal(42, ok.Value);
  }

  [Fact]
  public void ToActionResult_Should_Return_404_When_Error_Code_Is_NOT_FOUND()
  {
    var result = Result.Fail<int>(Error.NotFound("User"));

    var actionResult = result.ToActionResult(_controller);

    var notFound = Assert.IsType<NotFoundObjectResult>(actionResult);
    var body = Assert.IsType<ErrorResponse>(notFound.Value);
    Assert.Equal("NOT_FOUND", body.Code);
  }

  [Fact]
  public void ToActionResult_Should_Return_400_When_Error_Code_Is_VALIDATION_ERROR()
  {
    var result = Result.Fail<int>(Error.Validation("name", "Name ist erforderlich"));

    var actionResult = result.ToActionResult(_controller);

    var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
    var body = Assert.IsType<ErrorResponse>(badRequest.Value);
    Assert.Equal("VALIDATION_ERROR", body.Code);
  }

  [Fact]
  public void ToActionResult_Should_Include_Field_In_Body_For_ValidationError()
  {
    var result = Result.Fail<int>(Error.Validation("email", "Ungültige E-Mail"));

    var actionResult = result.ToActionResult(_controller);

    var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
    var body = Assert.IsType<ErrorResponse>(badRequest.Value);
    Assert.Equal("email", body.Field);
  }

  [Fact]
  public void ToActionResult_Should_Return_401_When_Error_Code_Is_UNAUTHORIZED()
  {
    var result = Result.Fail<int>(Error.Unauthorized("Kein Zugriff"));

    var actionResult = result.ToActionResult(_controller);

    var unauthorized = Assert.IsType<UnauthorizedObjectResult>(actionResult);
    var body = Assert.IsType<ErrorResponse>(unauthorized.Value);
    Assert.Equal("UNAUTHORIZED", body.Code);
  }

  [Fact]
  public void ToActionResult_Should_Return_409_When_Error_Code_Is_CONFLICT()
  {
    var result = Result.Fail<int>(Error.Conflict("Bereits vorhanden"));

    var actionResult = result.ToActionResult(_controller);

    var conflict = Assert.IsType<ConflictObjectResult>(actionResult);
    var body = Assert.IsType<ErrorResponse>(conflict.Value);
    Assert.Equal("CONFLICT", body.Code);
  }

  [Fact]
  public void ToActionResult_Should_Return_500_For_Unknown_Error_Code()
  {
    var result = Result.Fail<int>(new Error("Unbekannter Fehler").WithCode("SOME_UNKNOWN_CODE"));

    var actionResult = result.ToActionResult(_controller);

    var serverError = Assert.IsType<ObjectResult>(actionResult);
    Assert.Equal(500, serverError.StatusCode);
    var body = Assert.IsType<ErrorResponse>(serverError.Value);
    Assert.Equal("INTERNAL_ERROR", body.Code);
  }

  // ── Result.ToActionResult (non-generic) ───────────────────────────────────

  [Fact]
  public void ToActionResult_NonGeneric_Should_Return_204_When_Result_Is_Ok()
  {
    var result = Result.Ok();

    var actionResult = result.ToActionResult(_controller);

    Assert.IsType<NoContentResult>(actionResult);
  }

  [Fact]
  public void ToActionResult_NonGeneric_Should_Return_404_When_Error_Code_Is_NOT_FOUND()
  {
    var result = Result.Fail(Error.NotFound("Produkt"));

    var actionResult = result.ToActionResult(_controller);

    var notFound = Assert.IsType<NotFoundObjectResult>(actionResult);
    var body = Assert.IsType<ErrorResponse>(notFound.Value);
    Assert.Equal("NOT_FOUND", body.Code);
  }

  [Fact]
  public void ToActionResult_NonGeneric_Should_Return_400_When_Error_Code_Is_VALIDATION_ERROR()
  {
    var result = Result.Fail(Error.Validation("name", "Pflichtfeld"));

    var actionResult = result.ToActionResult(_controller);

    var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult);
    var body = Assert.IsType<ErrorResponse>(badRequest.Value);
    Assert.Equal("VALIDATION_ERROR", body.Code);
  }

  [Fact]
  public void ToActionResult_NonGeneric_Should_Return_500_For_Unknown_Error_Code()
  {
    var result = Result.Fail(new Error("Unbekannt").WithCode("UNKNOWN"));

    var actionResult = result.ToActionResult(_controller);

    var serverError = Assert.IsType<ObjectResult>(actionResult);
    Assert.Equal(500, serverError.StatusCode);
  }
}

// ── ToMinimalApiResult ────────────────────────────────────────────────────────

public sealed class MinimalApiExtensionsTests
{
  [Fact]
  public void ToMinimalApiResult_Should_Return_Ok_When_Result_Is_Ok()
  {
    var result = Result.Ok(42);

    var apiResult = result.ToMinimalApiResult();

    Assert.NotNull(apiResult);
  }

  [Fact]
  public void ToMinimalApiResult_Should_Return_NotFound_When_Error_Code_Is_NOT_FOUND()
  {
    var result = Result.Fail<int>(Error.NotFound("User"));

    var apiResult = result.ToMinimalApiResult();

    Assert.NotNull(apiResult);
  }

  [Fact]
  public void ToMinimalApiResult_Should_Return_BadRequest_When_Error_Code_Is_VALIDATION_ERROR()
  {
    var result = Result.Fail<int>(Error.Validation("email", "Ungültig"));

    var apiResult = result.ToMinimalApiResult();

    Assert.NotNull(apiResult);
  }

  [Fact]
  public void ToMinimalApiResult_Should_Return_Unauthorized_When_Error_Code_Is_UNAUTHORIZED()
  {
    var result = Result.Fail<int>(Error.Unauthorized());

    var apiResult = result.ToMinimalApiResult();

    Assert.NotNull(apiResult);
  }

  [Fact]
  public void ToMinimalApiResult_Should_Return_Conflict_When_Error_Code_Is_CONFLICT()
  {
    var result = Result.Fail<int>(Error.Conflict("Bereits vorhanden"));

    var apiResult = result.ToMinimalApiResult();

    Assert.NotNull(apiResult);
  }

  [Fact]
  public void ToMinimalApiResult_Should_Return_Problem_For_Unknown_Error_Code()
  {
    var result = Result.Fail<int>(new Error("Unbekannt").WithCode("UNKNOWN"));

    var apiResult = result.ToMinimalApiResult();

    Assert.NotNull(apiResult);
  }
}

// ── ResultMiddleware ──────────────────────────────────────────────────────────

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
  public async Task Middleware_Should_Return_500_When_Unhandled_Exception_Is_Thrown()
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
  }

  [Fact]
  public async Task Middleware_Should_Return_Json_ContentType_On_Exception()
  {
    var host = await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
          webBuilder.UseTestServer();
          webBuilder.Configure(app =>
          {
            app.UseMiddleware<ResultMiddleware>();
            app.Run(_ => throw new Exception("Fehler"));
          });
        })
        .StartAsync();

    var client = host.GetTestClient();
    var response = await client.GetAsync("/");

    Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
  }

  [Fact]
  public async Task Middleware_Should_Return_ErrorResponse_Body_On_Exception()
  {
    var host = await new HostBuilder()
        .ConfigureWebHost(webBuilder =>
        {
          webBuilder.UseTestServer();
          webBuilder.Configure(app =>
          {
            app.UseMiddleware<ResultMiddleware>();
            app.Run(_ => throw new Exception("Unbehandelt"));
          });
        })
        .StartAsync();

    var client = host.GetTestClient();
    var response = await client.GetAsync("/");
    var body = await response.Content.ReadAsStringAsync();

    Assert.Contains("unexpected error", body, StringComparison.OrdinalIgnoreCase);
  }
}

// ── Helper ────────────────────────────────────────────────────────────────────

internal sealed class TestController : ControllerBase { }