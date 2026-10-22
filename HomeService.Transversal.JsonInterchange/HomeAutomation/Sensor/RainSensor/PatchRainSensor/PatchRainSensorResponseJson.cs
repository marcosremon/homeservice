using System.Text.Json.Serialization;
using HomeService.Transversal.Wrapper.Json;

namespace HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.RainSensor.PatchRainSensor;

public class PatchRainSensorResponseJson : BaseResponseJson
{
    [JsonPropertyName("rainStarted")]
    public bool RainStarted { get; set; }
}
