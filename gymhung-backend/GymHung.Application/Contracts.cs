using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Metadata;
namespace GymHung.Application;

public sealed class ApiException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
public static class GymClock
{
    public static DateTime Now
    {
        get
        {
            var value = DateTime.UtcNow.AddHours(7);
            return new DateTime(value.Ticks - value.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Unspecified);
        }
    }
    public static DateOnly Today => DateOnly.FromDateTime(Now);
}
public sealed record MutationRequest(JsonElement Draft, JsonElement? UserDraft = null);
public static class ResourceDto
{
    public static Dictionary<string, object?> From(object entity, IEntityType type) => type.GetProperties().Where(p => p.Name != "PasswordHash").ToDictionary(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name), p => Value(p.PropertyInfo?.GetValue(entity)));
    public static object? Value(object? value) => value switch { long n => n.ToString(CultureInfo.InvariantCulture), decimal n => n.ToString(CultureInfo.InvariantCulture), _ => value };
}
public sealed class LongStringConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => r.TokenType == JsonTokenType.String ? long.Parse(r.GetString()!, CultureInfo.InvariantCulture) : r.GetInt64(); public override void Write(Utf8JsonWriter w, long v, JsonSerializerOptions o) => w.WriteStringValue(v.ToString(CultureInfo.InvariantCulture));
}
public sealed class DecimalStringConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader r, Type t, JsonSerializerOptions o) => r.TokenType == JsonTokenType.String ? decimal.Parse(r.GetString()!, CultureInfo.InvariantCulture) : r.GetDecimal(); public override void Write(Utf8JsonWriter w, decimal v, JsonSerializerOptions o) => w.WriteStringValue(v.ToString(CultureInfo.InvariantCulture));
}

