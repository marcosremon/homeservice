namespace HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.PatchRainSensor;

public class PatchRainSensorRequest
{
    public string CallOut { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public int AdcValue { get; set; } = 4095;
    public int WetnessPercent { get; set; }
    public bool IsRaining { get; set; }
}
