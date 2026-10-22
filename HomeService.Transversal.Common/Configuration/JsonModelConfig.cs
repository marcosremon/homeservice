using System.Text.Json;
using System.Text.Json.Serialization;

namespace HomeService.Transversal.Common.Configuration;

// Equivalente al ConfigDict de pydantic de este proyecto: los alias viajan en
// [JsonPropertyName] y el exclude_none se traduce en WhenWritingNull.
public static class JsonModelConfig
{
    public static readonly JsonSerializerOptions Default = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
