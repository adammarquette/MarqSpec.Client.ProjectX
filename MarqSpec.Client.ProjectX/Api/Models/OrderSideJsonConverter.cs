using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Reads response <c>side</c> as a nullable <see cref="OrderSide"/> without treating
/// JSON <c>null</c> as a missing field.
/// </summary>
/// <remarks>
/// An omitted property never reaches this converter, so the backing
/// <c>OrderSide?</c> stays <c>null</c>. An explicit <c>null</c> token is rejected
/// because swagger types <c>side</c> as <c>OrderSide</c>, not nullable-and-present.
/// Integers — including out-of-range values — are cast, matching the REST layer's
/// <see cref="System.Text.Json.JsonSerializerDefaults.Web"/> behaviour with no
/// string-enum converter (ADR-0001, gh#83).
/// </remarks>
internal sealed class OrderSideJsonConverter : JsonConverter<OrderSide?>
{
    /// <inheritdoc />
    /// <remarks>
    /// <see cref="JsonSerializer"/> otherwise assigns <c>null</c> without calling
    /// <see cref="Read"/> when the token is JSON <c>null</c>.
    /// </remarks>
    public override bool HandleNull => true;

    public override OrderSide? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            throw new JsonException("The JSON value could not be converted to OrderSide.");
        }

        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var value))
        {
            return (OrderSide)value;
        }

        throw new JsonException("The JSON value could not be converted to OrderSide.");
    }

    public override void Write(Utf8JsonWriter writer, OrderSide? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteNumberValue((int)value.Value);
    }
}
