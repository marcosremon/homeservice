namespace HomeService.Domain.Model.Entity;

public class Device
{
    public long DeviceId { get; set; }
    public long HouseZoneId { get; set; }
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
}
