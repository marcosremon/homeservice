using System.Text.Json.Serialization;

namespace HomeService.Transversal.Common.Roomba.Payload;

public class RoombaRegionParams
{
    [JsonPropertyName("noAutoPasses")]
    public bool NoAutoPasses { get; set; }

    [JsonPropertyName("twoPass")]
    public bool TwoPass { get; set; }
}
