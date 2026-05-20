using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Resulta.Json
{
  /// <summary>
  /// JSON converter factory that produces converters for <see cref="Result"/>, <see cref="Result{T}"/>,
  /// and <see cref="Error"/>. Register via <see cref="ResultaJsonOptionsExtensions.AddResultaConverters"/>.
  /// </summary>
  /// <remarks>
  /// The produced JSON shape is discriminated by an <c>isSuccess</c> property and omits any data that
  /// could leak server internals (no exception stack traces, no full metadata dictionary). Failure JSON
  /// includes <c>message</c>, optional <c>code</c>, optional <c>field</c> (from <see cref="Error.Metadata"/>),
  /// optional <c>exceptionMessage</c> (no stack trace), and a recursive <c>causedBy</c> chain truncated at
  /// three levels.
  /// </remarks>
  public sealed class ResultaJsonConverterFactory : JsonConverterFactory
  {
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
      ArgumentNullException.ThrowIfNull(typeToConvert);
      if (typeToConvert == typeof(Result)) return true;
      if (typeToConvert == typeof(Error)) return true;
      return typeToConvert.IsGenericType
          && typeToConvert.GetGenericTypeDefinition() == typeof(Result<>);
    }

    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
      ArgumentNullException.ThrowIfNull(typeToConvert);
      if (typeToConvert == typeof(Result)) return new ResultJsonConverter();
      if (typeToConvert == typeof(Error)) return new ErrorJsonConverter();

      var elementType = typeToConvert.GetGenericArguments()[0];
      var converterType = typeof(ResultJsonConverter<>).MakeGenericType(elementType);
      return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
  }

  /// <summary>
  /// JSON converter for the non-generic <see cref="Result"/>.
  /// </summary>
  /// <remarks>
  /// Writes <c>{ "isSuccess": true }</c> on success and
  /// <c>{ "isSuccess": false, "error": { ... } }</c> on failure.
  /// </remarks>
  public sealed class ResultJsonConverter : JsonConverter<Result>
  {
    /// <inheritdoc/>
    public override Result Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if (reader.TokenType != JsonTokenType.StartObject)
        throw new JsonException("Expected start of object for Result.");

      bool? isSuccess = null;
      Error? error = null;

      while (reader.Read())
      {
        if (reader.TokenType == JsonTokenType.EndObject) break;
        if (reader.TokenType != JsonTokenType.PropertyName)
          throw new JsonException("Expected property name.");

        var propName = reader.GetString();
        reader.Read();

        if (JsonNames.Match(propName, "isSuccess", options))
          isSuccess = reader.GetBoolean();
        else if (JsonNames.Match(propName, "error", options))
          error = JsonSerializer.Deserialize<Error>(ref reader, options);
        else
          reader.Skip();
      }

      if (isSuccess is null)
        throw new JsonException("Result JSON is missing the 'isSuccess' discriminator.");
      if (isSuccess.Value) return Result.Ok();
      if (error is null)
        throw new JsonException("Result JSON has 'isSuccess: false' but no 'error' object.");
      return Result.Fail(error);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Result value, JsonSerializerOptions options)
    {
      ArgumentNullException.ThrowIfNull(writer);
      ArgumentNullException.ThrowIfNull(value);

      writer.WriteStartObject();
      writer.WriteBoolean(JsonNames.Name("isSuccess", options), value.IsSuccess);
      if (value.IsFailure)
      {
        writer.WritePropertyName(JsonNames.Name("error", options));
        JsonSerializer.Serialize(writer, value.Error, options);
      }
      writer.WriteEndObject();
    }
  }

  /// <summary>
  /// JSON converter for <see cref="Result{T}"/>.
  /// </summary>
  /// <remarks>
  /// Writes <c>{ "isSuccess": true, "value": &lt;T&gt; }</c> on success and
  /// <c>{ "isSuccess": false, "error": { ... } }</c> on failure.
  /// </remarks>
  /// <typeparam name="T">The success value type.</typeparam>
  public sealed class ResultJsonConverter<T> : JsonConverter<Result<T>>
  {
    /// <inheritdoc/>
    public override Result<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if (reader.TokenType != JsonTokenType.StartObject)
        throw new JsonException("Expected start of object for Result<T>.");

      bool? isSuccess = null;
      bool sawValue = false;
      T? value = default;
      Error? error = null;

      while (reader.Read())
      {
        if (reader.TokenType == JsonTokenType.EndObject) break;
        if (reader.TokenType != JsonTokenType.PropertyName)
          throw new JsonException("Expected property name.");

        var propName = reader.GetString();
        reader.Read();

        if (JsonNames.Match(propName, "isSuccess", options))
        {
          isSuccess = reader.GetBoolean();
        }
        else if (JsonNames.Match(propName, "value", options))
        {
          value = JsonSerializer.Deserialize<T>(ref reader, options);
          sawValue = true;
        }
        else if (JsonNames.Match(propName, "error", options))
        {
          error = JsonSerializer.Deserialize<Error>(ref reader, options);
        }
        else
        {
          reader.Skip();
        }
      }

      if (isSuccess is null)
        throw new JsonException("Result<T> JSON is missing the 'isSuccess' discriminator.");
      if (isSuccess.Value)
      {
        if (!sawValue)
          throw new JsonException("Result<T> JSON has 'isSuccess: true' but no 'value' field.");
        return Result<T>.Ok(value!);
      }
      if (error is null)
        throw new JsonException("Result<T> JSON has 'isSuccess: false' but no 'error' object.");
      return Result<T>.Fail(error);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Result<T> value, JsonSerializerOptions options)
    {
      ArgumentNullException.ThrowIfNull(writer);
      ArgumentNullException.ThrowIfNull(value);

      writer.WriteStartObject();
      writer.WriteBoolean(JsonNames.Name("isSuccess", options), value.IsSuccess);
      if (value.IsSuccess)
      {
        writer.WritePropertyName(JsonNames.Name("value", options));
        JsonSerializer.Serialize(writer, value.Value, options);
      }
      else
      {
        writer.WritePropertyName(JsonNames.Name("error", options));
        JsonSerializer.Serialize(writer, value.Error, options);
      }
      writer.WriteEndObject();
    }
  }

  /// <summary>
  /// JSON converter for <see cref="Error"/>.
  /// </summary>
  /// <remarks>
  /// Writes <c>message</c>, optional <c>code</c>, optional <c>field</c> (from <see cref="Error.Metadata"/>),
  /// optional <c>exceptionMessage</c> (never the stack trace), and a recursive <c>causedBy</c> chain
  /// truncated at three levels to avoid pathological cycles. The exception object itself does not roundtrip
  /// on read — only the diagnostic message is written, and the deserialized error has
  /// <see cref="Error.Exception"/> set to <c>null</c>.
  /// </remarks>
  public sealed class ErrorJsonConverter : JsonConverter<Error>
  {
    private const int MaxCauseDepth = 3;
    private const string FieldMetadataKey = "field";

    /// <inheritdoc/>
    public override Error Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
      if (reader.TokenType != JsonTokenType.StartObject)
        throw new JsonException("Expected start of object for Error.");

      string? message = null;
      string? code = null;
      string? field = null;
      Error? causedBy = null;

      while (reader.Read())
      {
        if (reader.TokenType == JsonTokenType.EndObject) break;
        if (reader.TokenType != JsonTokenType.PropertyName)
          throw new JsonException("Expected property name.");

        var propName = reader.GetString();
        reader.Read();

        if (JsonNames.Match(propName, "message", options))
          message = reader.GetString();
        else if (JsonNames.Match(propName, "code", options))
          code = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
        else if (JsonNames.Match(propName, "field", options))
          field = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
        else if (JsonNames.Match(propName, "causedBy", options))
          causedBy = JsonSerializer.Deserialize<Error>(ref reader, options);
        else
          reader.Skip();
      }

      if (string.IsNullOrWhiteSpace(message))
        throw new JsonException("Error JSON is missing the required 'message' field.");

      Dictionary<string, object>? metadata = null;
      if (field is not null)
        metadata = new Dictionary<string, object> { [FieldMetadataKey] = field };

      return new Error(message, code, exception: null, causedBy: causedBy, metadata: metadata);
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, Error value, JsonSerializerOptions options)
    {
      ArgumentNullException.ThrowIfNull(writer);
      ArgumentNullException.ThrowIfNull(value);
      WriteAtDepth(writer, value, options, depth: 0);
    }

    private static void WriteAtDepth(Utf8JsonWriter writer, Error value, JsonSerializerOptions options, int depth)
    {
      writer.WriteStartObject();

      writer.WriteString(JsonNames.Name("message", options), value.Message);

      if (value.Code is not null)
        writer.WriteString(JsonNames.Name("code", options), value.Code);

      if (value.Metadata.TryGetValue(FieldMetadataKey, out var fieldObj) && fieldObj is string field)
        writer.WriteString(JsonNames.Name("field", options), field);

      if (value.Exception is not null)
        writer.WriteString(JsonNames.Name("exceptionMessage", options), value.Exception.Message);

      if (value.CausedBy is not null && depth + 1 < MaxCauseDepth)
      {
        writer.WritePropertyName(JsonNames.Name("causedBy", options));
        WriteAtDepth(writer, value.CausedBy, options, depth + 1);
      }

      writer.WriteEndObject();
    }
  }

  /// <summary>
  /// Extension methods for registering the Resulta JSON converters on a <see cref="JsonSerializerOptions"/>.
  /// </summary>
  public static class ResultaJsonOptionsExtensions
  {
    /// <summary>
    /// Adds the Resulta JSON converter factory to <paramref name="options"/> so that <see cref="Result"/>,
    /// <see cref="Result{T}"/>, and <see cref="Error"/> serialize and deserialize with a stable, leak-free
    /// JSON shape. Returns the same options instance for chaining.
    /// </summary>
    /// <param name="options">The serializer options to extend.</param>
    public static JsonSerializerOptions AddResultaConverters(this JsonSerializerOptions options)
    {
      ArgumentNullException.ThrowIfNull(options);
      options.Converters.Add(new ResultaJsonConverterFactory());
      return options;
    }
  }

  internal static class JsonNames
  {
    internal static string Name(string camelCaseName, JsonSerializerOptions options)
        => options.PropertyNamingPolicy?.ConvertName(camelCaseName) ?? camelCaseName;

    internal static bool Match(string? actual, string camelCaseExpected, JsonSerializerOptions options)
    {
      if (actual is null) return false;
      var expected = Name(camelCaseExpected, options);
      var comparison = options.PropertyNameCaseInsensitive
          ? StringComparison.OrdinalIgnoreCase
          : StringComparison.Ordinal;
      return string.Equals(actual, expected, comparison);
    }
  }
}
