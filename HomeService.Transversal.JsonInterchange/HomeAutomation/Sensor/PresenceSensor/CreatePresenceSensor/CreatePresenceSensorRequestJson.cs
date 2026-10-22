using System.Text.Json.Serialization;

namespace HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.PresenceSensor.CreatePresenceSensor;

public class CreatePresenceSensorRequestJson
{
    [JsonPropertyName("callout")]
    public string CallOut { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("deviceType")]
    public string DeviceType { get; set; } = string.Empty;

    [JsonPropertyName("ts")]
    public long Ts { get; set; }

    [JsonPropertyName("presence")]
    public bool Presence { get; set; }

    [JsonPropertyName("distanceCm")]
    public int DistanceCm { get; set; }

    [JsonPropertyName("motion")]
    public string Motion { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    [JsonPropertyName("macAddress")]
    public string MacAddress { get; set; } = string.Empty;
}
