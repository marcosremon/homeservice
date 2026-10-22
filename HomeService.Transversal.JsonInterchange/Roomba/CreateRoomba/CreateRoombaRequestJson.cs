using System.Text.Json.Serialization;

namespace HomeService.Transversal.JsonInterchange.Roomba.CreateRoomba;

public class CreateRoombaRequestJson
{
    [JsonPropertyName("callout")]
    public string CallOut { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("deviceType")]
    public string DeviceType { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    [JsonPropertyName("macAddress")]
    public string MacAddress { get; set; } = string.Empty;
}
