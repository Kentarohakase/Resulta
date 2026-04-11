using Resulta;

using Xunit;

namespace Resulta.Tests;

/// <summary>
/// Documents that <see cref="Result{T}.Ok"/> treats null reference values as successful outcomes.
/// </summary>
public sealed class ResultOkNullTests
{
  [Fact]
  public void Ok_ReferenceType_Null_Should_Be_Success()
  {
    var result = Result<string>.Ok(null!);

    Assert.True(result.IsSuccess);
    Assert.Null(result.Value);
  }

  [Fact]
  public void Ok_Static_Generic_ReferenceType_Null_Should_Be_Success()
  {
    var result = Result.Ok<string>(null!);

    Assert.True(result.IsSuccess);
    Assert.Null(result.Value);
  }

  [Fact]
  public void Implicit_Conversion_ReferenceType_Null_Should_Be_Success()
  {
    string? value = null;
    Result<string> result = value!;

    Assert.True(result.IsSuccess);
    Assert.Null(result.Value);
  }
}
