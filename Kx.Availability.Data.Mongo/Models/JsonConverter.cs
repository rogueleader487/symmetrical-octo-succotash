using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;

namespace Kx.Availability.Data.Mongo.Models;

/*
 * Doesn't belong here. A utility class like this would belong in a Common project
 * for reuse across multiple apps ensuring consistent conversions
*/
public class CustomDateTimeConverter : JsonConverter<DateTime>
{
  public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
  {
    Debug.Assert(typeToConvert == typeof(DateTime));
    return DateTime.Parse(reader.GetString() ?? string.Empty);
  }

  public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
  {
    Log.Information("DEBUG, using custom date time converter");
    writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
  }
}
