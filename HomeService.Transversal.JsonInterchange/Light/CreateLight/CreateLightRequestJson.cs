using System.Text.Json.Serialization;

namespace HomeService.Transversal.JsonInterchange.Light.CreateLight;

public class CreateLightRequestJson
{
    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; set; } = string.Empty;

    [JsonPropertyName("mqttTopic")]
    public string MqttTopic { get; set; } = string.Empty;

    [JsonPropertyName("isOn")]
    public bool IsOn { get; set; }

    [JsonPropertyName("brightness")]
    public int Brightness { get; set; }

    [JsonPropertyName("color")]
    public string Color { get; set; } = string.Empty;

    [JsonPropertyName("colorTemperature")]
    public int ColorTemperature { get; set; }
}
