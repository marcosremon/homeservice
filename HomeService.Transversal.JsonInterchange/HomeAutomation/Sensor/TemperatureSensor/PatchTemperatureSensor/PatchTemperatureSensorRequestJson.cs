using System.Text.Json.Serialization;

namespace HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.TemperatureSensor.PatchTemperatureSensor;

public class PatchTemperatureSensorRequestJson
{
    [JsonPropertyName("callOut")]
    public string CallOut { get; set; } = string.Empty;

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("deviceType")]
    public string DeviceType { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("macAddress")]
    public string MacAddress { get; set; } = string.Empty;

    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("adcVoltage")]
    public double? AdcVoltage { get; set; }

    [JsonPropertyName("measureAt")]
    public double? MeasureAt { get; set; }
}
