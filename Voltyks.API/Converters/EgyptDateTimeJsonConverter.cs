using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Voltyks.Persistence.Utilities;

namespace Voltyks.API.Converters
{
    /// <summary>
    /// Serializes DateTime values as ISO 8601 strings with the Egypt UTC offset,
    /// e.g. "2026-04-20T16:40:00+02:00". This tells the client exactly which
    /// timezone the value is in so it doesn't misinterpret the timestamp.
    ///
    /// Deserialization accepts any ISO 8601 string and returns a DateTime
    /// representing the Egypt local time.
    /// </summary>
    public class EgyptDateTimeJsonConverter : JsonConverter<DateTime>
    {
        private const string Format = "yyyy-MM-ddTHH:mm:ss.fffzzz";

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return default;

            var str = reader.GetString();
            if (string.IsNullOrWhiteSpace(str))
                return default;

            if (DateTimeOffset.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dto))
                return DateTimeHelper.ToEgyptTime(dto.UtcDateTime);

            if (DateTime.TryParse(str, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                return dt;

            throw new JsonException($"Unable to parse '{str}' as DateTime.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var offset = DateTimeHelper.GetEgyptUtcOffset(DateTime.UtcNow);
            // The stored value already represents Egypt local time (Unspecified kind).
            // Emit with the Egypt offset so clients treat it correctly.
            var dto = new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Unspecified), offset);
            writer.WriteStringValue(dto.ToString(Format, CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Same as <see cref="EgyptDateTimeJsonConverter"/> but for nullable DateTime.
    /// </summary>
    public class EgyptNullableDateTimeJsonConverter : JsonConverter<DateTime?>
    {
        private static readonly EgyptDateTimeJsonConverter Inner = new();

        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            return Inner.Read(ref reader, typeof(DateTime), options);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value is null) { writer.WriteNullValue(); return; }
            Inner.Write(writer, value.Value, options);
        }
    }
}
