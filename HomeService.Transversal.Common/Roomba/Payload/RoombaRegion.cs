using System.Text.Json.Serialization;

namespace HomeService.Transversal.Common.Roomba.Payload;

public class RoombaRegion
{
    [JsonPropertyName("region_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RegionId { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "rid";

    [JsonPropertyName("params")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public RoombaRegionParams? Params { get; set; }
}
