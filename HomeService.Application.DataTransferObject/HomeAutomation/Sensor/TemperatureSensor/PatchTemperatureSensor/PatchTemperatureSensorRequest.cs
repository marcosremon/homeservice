namespace HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.PatchTemperatureSensor;

public class PatchTemperatureSensorRequest
{
    public string CallOut { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public double? Temperature { get; set; }
    public double? AdcVoltage { get; set; }
    public double? MeasureAt { get; set; }
}
