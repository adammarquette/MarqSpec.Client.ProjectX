using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarqSpec.Client.ProjectX.Api.Models;

/// <summary>
/// Maps the ProjectX trade-direction wire onto a nullable <see cref="TradeLogType"/>.
/// </summary>
/// <remarks>
/// Wire <c>0</c> is Buy and <c>1</c> is Sell. Anything else — omitted, null,
/// an unrecognised integer, or an unparseable token — becomes <c>null</c> so
/// it cannot be mistaken for a stated Buy (gh#86). Throwing would drop the
/// whole trade at the hub bind, which is worse than an unstated direction.
/// </remarks>
internal sealed class TradeLogTypeJsonConverter : JsonConverter<TradeLogType?>
{
    public override TradeLogType? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.Number:
                return reader.TryGetInt32(out var value) ? Map(value) : null;
            case JsonTokenType.String:
                var text = reader.GetString();
                return int.TryParse(text, out var parsed) ? Map(parsed) : null;
            default:
                reader.Skip();
                return null;
        }
    }

    public override void Write(Utf8JsonWriter writer, TradeLogType? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteNumberValue((int)value.Value);
    }

    private static TradeLogType? Map(int wire) => wire switch
    {
        0 => TradeLogType.Buy,
        1 => TradeLogType.Sell,
        _ => null,
    };
}
