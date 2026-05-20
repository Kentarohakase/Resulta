using System;
using System.Linq;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Resulta.AspNetCore.OpenApi;

using Xunit;

namespace Resulta.Tests.OpenApi;

public sealed class RouteHandlerBuilderExtensionsTests
{
  private static WebApplication BuildApp() => WebApplication.CreateBuilder(Array.Empty<string>()).Build();

  private static IProducesResponseTypeMetadata[] MetadataFor(Action<RouteHandlerBuilder> configure)
  {
    var app = BuildApp();
    var builder = app.MapGet("/test", () => Results.Ok());
    configure(builder);

    var routeBuilder = (IEndpointRouteBuilder)app;
    var endpoint = routeBuilder.DataSources.SelectMany(ds => ds.Endpoints).First();
    return endpoint.Metadata.OfType<IProducesResponseTypeMetadata>().ToArray();
  }

  [Fact]
  public void ProducesResultaErrors_Default_Registers_All_Five_Status_Codes()
  {
    var metadata = MetadataFor(b => b.ProducesResultaErrors());

    var statusCodes = metadata.Select(m => m.StatusCode).Distinct().ToHashSet();

    Assert.Contains(StatusCodes.Status400BadRequest, statusCodes);
    Assert.Contains(StatusCodes.Status401Unauthorized, statusCodes);
    Assert.Contains(StatusCodes.Status404NotFound, statusCodes);
    Assert.Contains(StatusCodes.Status409Conflict, statusCodes);
    Assert.Contains(StatusCodes.Status500InternalServerError, statusCodes);
  }

  [Fact]
  public void ProducesResultaErrors_Should_Map_400_To_HttpValidationProblemDetails()
  {
    var metadata = MetadataFor(b => b.ProducesResultaErrors());

    var badRequest = metadata.Single(m => m.StatusCode == StatusCodes.Status400BadRequest);
    Assert.Equal(typeof(HttpValidationProblemDetails), badRequest.Type);
  }

  [Fact]
  public void ProducesResultaErrors_Should_Use_ProblemDetails_For_Non_Validation_Codes()
  {
    var metadata = MetadataFor(b => b.ProducesResultaErrors());

    foreach (var code in new[] { 401, 404, 409, 500 })
    {
      var entry = metadata.Single(m => m.StatusCode == code);
      Assert.Equal(typeof(ProblemDetails), entry.Type);
    }
  }

  [Fact]
  public void ProducesResultaErrors_Should_Use_ProblemJson_ContentType()
  {
    var metadata = MetadataFor(b => b.ProducesResultaErrors());

    foreach (var entry in metadata)
      Assert.Contains("application/problem+json", entry.ContentTypes);
  }

  [Fact]
  public void ProducesResultaErrors_With_Subset_Should_Register_Only_Selected_Codes()
  {
    var metadata = MetadataFor(b => b.ProducesResultaErrors(404, 409));

    var statusCodes = metadata.Select(m => m.StatusCode).Distinct().ToHashSet();
    Assert.Contains(404, statusCodes);
    Assert.Contains(409, statusCodes);
    Assert.DoesNotContain(400, statusCodes);
    Assert.DoesNotContain(401, statusCodes);
    Assert.DoesNotContain(500, statusCodes);
  }
}
