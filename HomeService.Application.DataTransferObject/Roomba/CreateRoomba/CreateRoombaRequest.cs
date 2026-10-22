namespace HomeService.Application.DataTransferObject.Roomba.CreateRoomba;

public class CreateRoombaRequest
{
    public string CallOut { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Manufacturer { get; set; } = string.Empty;
    public string MacAddress { get; set; } = string.Empty;
}
