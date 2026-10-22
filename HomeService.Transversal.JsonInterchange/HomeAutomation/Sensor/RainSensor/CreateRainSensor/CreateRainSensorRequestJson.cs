using System.Text.Json.Serialization;

namespace HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.RainSensor.CreateRainSensor;

public class CreateRainSensorRequestJson
{
    [JsonPropertyName("callOut")]
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

    [JsonPropertyName("adcValue")]
    public int AdcValue { get; set; } = 4095;

    [JsonPropertyName("wetnessPercent")]
    public int WetnessPercent { get; set; }

    [JsonPropertyName("isRaining")]
    public bool IsRaining { get; set; }
}
