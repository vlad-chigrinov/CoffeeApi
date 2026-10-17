using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CoffeeApi.Api.Data;

public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.ToUniversalTime(),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
