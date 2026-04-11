using System.Collections.Generic;

using Resulta;

using Xunit;

namespace Resulta.Tests;

public sealed class ErrorMetadataCopyTests
{
  [Fact]
  public void Error_Constructor_Should_Copy_Metadata_So_Caller_Mutations_Do_Not_Affect_Error()
  {
    var source = new Dictionary<string, object> { ["key"] = "original" };
    var error = new Error("msg", metadata: source);

    source["key"] = "mutated";
    source["extra"] = 1;

    Assert.Equal("original", error.Metadata["key"]);
    Assert.False(error.Metadata.ContainsKey("extra"));
  }

  [Fact]
  public void Error_WithMetadata_Should_Not_Share_Dictionary_With_Previous_Instance()
  {
    var error = new Error("a", metadata: new Dictionary<string, object> { ["k"] = 1 });
    var next = error.WithMetadata("k", 2);

    Assert.Equal(1, error.Metadata["k"]);
    Assert.Equal(2, next.Metadata["k"]);
  }
}
