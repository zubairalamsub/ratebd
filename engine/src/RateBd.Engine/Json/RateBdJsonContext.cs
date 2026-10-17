using System.Text.Json.Serialization;
using RateBd.Engine.Models;

namespace RateBd.Engine.Json;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true)]
[JsonSerializable(typeof(CorridorSnapshot))]
internal sealed partial class RateBdJsonContext : JsonSerializerContext;
