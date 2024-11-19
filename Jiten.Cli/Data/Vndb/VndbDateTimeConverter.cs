using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jiten.Cli.Data.Vndb
{
    public class VndbDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();
                if (DateTime.TryParse(stringValue, out DateTime date))
                    return date;

                if (int.TryParse(stringValue, out int year))
                    return new DateTime(year, 1, 1);
            }
            
            if (reader.TokenType == JsonTokenType.Null)
            {
                return default;
            }

            throw new JsonException("Invalid date format");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
        }
    }
}