using System;
using System.Text.Json;

using Resulta;
using Resulta.Json;

using Xunit;

namespace Resulta.Tests.Json;

public sealed class ResultaJsonConvertersTests
{
  private static JsonSerializerOptions Options() =>
      new JsonSerializerOptions().AddResultaConverters();

  private static JsonSerializerOptions CamelCase() =>
      new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }.AddResultaConverters();

  // ── Result (non-generic) ─────────────────────────────────────────────

  [Fact]
  public void Result_Ok_Should_Roundtrip()
  {
    var opts = Options();
    var original = Result.Ok();

    var json = JsonSerializer.Serialize(original, opts);
    var roundtripped = JsonSerializer.Deserialize<Result>(json, opts)!;

    Assert.Contains("\"isSuccess\":true", json);
    Assert.True(roundtripped.IsSuccess);
  }

  [Fact]
  public void Result_Fail_Should_Roundtrip_Message_And_Code()
  {
    var opts = Options();
    var original = Result.Fail(Error.NotFound("User"));

    var json = JsonSerializer.Serialize(original, opts);
    var roundtripped = JsonSerializer.Deserialize<Result>(json, opts)!;

    Assert.True(roundtripped.IsFailure);
    Assert.Equal("'User' was not found.", roundtripped.Error.Message);
    Assert.Equal("NOT_FOUND", roundtripped.Error.Code);
  }

  // ── Result<T> ────────────────────────────────────────────────────────

  [Fact]
  public void ResultT_Ok_Should_Roundtrip_Value()
  {
    var opts = Options();
    var original = Result<int>.Ok(42);

    var json = JsonSerializer.Serialize(original, opts);
    var roundtripped = JsonSerializer.Deserialize<Result<int>>(json, opts)!;

    Assert.True(roundtripped.IsSuccess);
    Assert.Equal(42, roundtripped.Value);
  }

  [Fact]
  public void ResultT_Ok_String_Should_Roundtrip()
  {
    var opts = Options();
    var original = Result<string>.Ok("hello");

    var json = JsonSerializer.Serialize(original, opts);
    var roundtripped = JsonSerializer.Deserialize<Result<string>>(json, opts)!;

    Assert.True(roundtripped.IsSuccess);
    Assert.Equal("hello", roundtripped.Value);
  }

  [Fact]
  public void ResultT_Ok_Null_Reference_Value_Should_Roundtrip()
  {
    var opts = Options();
    var original = Result<string?>.Ok(null);

    var json = JsonSerializer.Serialize(original, opts);
    var roundtripped = JsonSerializer.Deserialize<Result<string?>>(json, opts)!;

    Assert.True(roundtripped.IsSuccess);
    Assert.Null(roundtripped.Value);
  }

  [Fact]
  public void ResultT_Ok_Complex_Object_Should_Roundtrip()
  {
    var opts = Options();
    var original = Result<UserDto>.Ok(new UserDto(1, "Alice"));

    var json = JsonSerializer.Serialize(original, opts);
    var roundtripped = JsonSerializer.Deserialize<Result<UserDto>>(json, opts)!;

    Assert.True(roundtripped.IsSuccess);
    Assert.Equal(1, roundtripped.Value.Id);
    Assert.Equal("Alice", roundtripped.Value.Name);
  }

  [Fact]
  public void ResultT_Fail_Validation_Should_Preserve_Field_Metadata()
  {
    var opts = Options();
    var original = Result<int>.Fail(Error.Validation("email", "Invalid format"));

    var json = JsonSerializer.Serialize(original, opts);
    var roundtripped = JsonSerializer.Deserialize<Result<int>>(json, opts)!;

    Assert.True(roundtripped.IsFailure);
    Assert.Equal("VALIDATION_ERROR", roundtripped.Error.Code);
    Assert.True(roundtripped.Error.Metadata.ContainsKey("field"));
    Assert.Equal("email", roundtripped.Error.Metadata["field"]);
  }

  [Fact]
  public void ResultT_Fail_Should_Not_Include_Value_In_Json()
  {
    var opts = Options();
    var original = Result<int>.Fail(Error.NotFound("User"));

    var json = JsonSerializer.Serialize(original, opts);

    Assert.DoesNotContain("\"value\"", json);
  }

  // ── Error: caused-by chain ───────────────────────────────────────────

  [Fact]
  public void Error_CausedBy_Chain_Should_Roundtrip()
  {
    var opts = Options();
    var inner = new Error("Database connection failed").WithCode("DB_DOWN");
    var original = Error.NotFound("User").WithCause(inner);

    var json = JsonSerializer.Serialize(original, opts);
    var roundtripped = JsonSerializer.Deserialize<Error>(json, opts)!;

    Assert.Equal("NOT_FOUND", roundtripped.Code);
    Assert.NotNull(roundtripped.CausedBy);
    Assert.Equal("Database connection failed", roundtripped.CausedBy!.Message);
    Assert.Equal("DB_DOWN", roundtripped.CausedBy!.Code);
  }

  [Fact]
  public void Error_CausedBy_Should_Be_Truncated_Below_Three_Levels_Deep()
  {
    var opts = Options();
    var l3 = new Error("Level 3");
    var l2 = new Error("Level 2").WithCause(l3);
    var l1 = new Error("Level 1").WithCause(l2);
    var root = new Error("Root").WithCause(l1);

    var json = JsonSerializer.Serialize(root, opts);

    Assert.Contains("Level 1", json);
    Assert.Contains("Level 2", json);
    Assert.DoesNotContain("Level 3", json);
  }

  // ── Exception handling ───────────────────────────────────────────────

  [Fact]
  public void Error_With_Exception_Should_Include_Message_But_Not_StackTrace()
  {
    var opts = Options();
    Exception ex;
    try
    {
      throw new InvalidOperationException("Boom in TopMethod");
    }
    catch (Exception caught)
    {
      ex = caught;
    }

    var error = Error.Unexpected(ex);
    var json = JsonSerializer.Serialize(error, opts);

    Assert.Contains("Boom in TopMethod", json);
    Assert.DoesNotContain("at ", json);
    Assert.DoesNotContain("StackTrace", json);
    Assert.DoesNotContain("InvalidOperationException", json);
  }

  [Fact]
  public void Error_Deserialized_Should_Drop_Exception_Object()
  {
    var opts = Options();
    var original = Error.Unexpected(new InvalidOperationException("Boom"));

    var json = JsonSerializer.Serialize(original, opts);
    var roundtripped = JsonSerializer.Deserialize<Error>(json, opts)!;

    Assert.Equal("UNEXPECTED_ERROR", roundtripped.Code);
    Assert.Null(roundtripped.Exception);
  }

  // ── Naming policy ────────────────────────────────────────────────────

  [Fact]
  public void CamelCase_Policy_Should_Produce_CamelCase_Keys()
  {
    var opts = CamelCase();
    var original = Result<int>.Fail(Error.Validation("email", "Invalid"));

    var json = JsonSerializer.Serialize(original, opts);

    Assert.Contains("\"isSuccess\":false", json);
    Assert.Contains("\"error\":", json);
    Assert.Contains("\"message\":", json);
    Assert.Contains("\"code\":", json);
    Assert.Contains("\"field\":", json);
  }

  [Fact]
  public void Default_Options_Should_Produce_CamelCase_Keys()
  {
    var opts = Options();
    var original = Result<int>.Ok(7);

    var json = JsonSerializer.Serialize(original, opts);

    Assert.Contains("\"isSuccess\":true", json);
    Assert.Contains("\"value\":7", json);
  }

  // ── Error cases ──────────────────────────────────────────────────────

  [Fact]
  public void Read_Should_Throw_When_IsSuccess_Missing()
  {
    var opts = Options();
    var malformed = "{}";

    Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Result>(malformed, opts));
  }

  [Fact]
  public void Read_Should_Throw_When_Failure_Has_No_Error()
  {
    var opts = Options();
    var malformed = "{\"isSuccess\":false}";

    Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Result>(malformed, opts));
  }

  [Fact]
  public void Read_Should_Throw_When_Success_ResultT_Has_No_Value()
  {
    var opts = Options();
    var malformed = "{\"isSuccess\":true}";

    Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Result<int>>(malformed, opts));
  }

  [Fact]
  public void Read_Should_Throw_When_Error_Missing_Message()
  {
    var opts = Options();
    var malformed = "{\"isSuccess\":false,\"error\":{\"code\":\"X\"}}";

    Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Result>(malformed, opts));
  }

  private sealed record UserDto(int Id, string Name);
}
