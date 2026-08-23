using System.Text.Json;
using System.Text.Json.Serialization;

namespace APIPlatform.Playground.Infrastructure;

/// <summary>
/// Without this, System.Text.Json deserializes every value in an
/// IReadOnlyDictionary&lt;string, object?&gt; property (DynamicQueryRequest.Filters,
/// DynamicInsertRequest.Values, DynamicMigrationRequest.FixedValues/SourceFilters, ...) as a boxed
/// JsonElement instead of a real CLR value — Dapper then has no DbType for it and every dynamic
/// write/filter call fails with "The member '...' of type System.Text.Json.JsonElement cannot be
/// used as a parameter value." Registered globally (Program.cs AddJsonOptions) so it applies to
/// every Dynamic* request shape without each one needing its own converter attribute.
/// </summary>
public sealed class ObjectToInferredTypesConverter : JsonConverter<object?>
{
    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True => true,
            JsonTokenType.False => false,
            JsonTokenType.Number when reader.TryGetInt64(out var l) => l,
            JsonTokenType.Number => reader.GetDouble(),
            JsonTokenType.String when reader.TryGetDateTime(out var dt) => dt,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Null => null,
            _ => JsonDocument.ParseValue(ref reader).RootElement.Clone()
        };

    public override void Write(Utf8JsonWriter writer, object? value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, value?.GetType() ?? typeof(object));
}
