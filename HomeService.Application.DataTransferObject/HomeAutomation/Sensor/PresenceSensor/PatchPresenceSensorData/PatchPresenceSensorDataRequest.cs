namespace HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.PatchPresenceSensorData;

public class PatchPresenceSensorDataRequest
{
    public string CallOut { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
    public long Ts { get; set; }
    public bool Presence { get; set; }
    public int DistanceCm { get; set; }
    public string Motion { get; set; } = string.Empty;
}
