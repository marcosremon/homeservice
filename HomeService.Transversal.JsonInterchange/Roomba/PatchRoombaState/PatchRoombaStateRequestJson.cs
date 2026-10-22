using System.Text.Json.Serialization;

namespace HomeService.Transversal.JsonInterchange.Roomba.PatchRoombaState;

public class PatchRoombaStateRequestJson
{
    [JsonPropertyName("isActivation")]
    public bool IsActivation { get; set; }

    [JsonPropertyName("isFinished")]
    public bool IsFinished { get; set; }

    [JsonPropertyName("target")]
    public string Target { get; set; } = string.Empty;

    [JsonPropertyName("phase")]
    public string Phase { get; set; } = string.Empty;

    [JsonPropertyName("batteryPercent")]
    public int BatteryPercent { get; set; }

    [JsonPropertyName("binFull")]
    public bool BinFull { get; set; }

    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;

    [JsonPropertyName("pmapId")]
    public string PmapId { get; set; } = string.Empty;

    [JsonPropertyName("userPmapvId")]
    public string UserPmapvId { get; set; } = string.Empty;

    [JsonPropertyName("isOnline")]
    public bool IsOnline { get; set; }
}
