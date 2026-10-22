using HomeService.Domain.Model.Enum.Light;

namespace HomeService.Application.DataTransferObject.Light;

public class LightDto
{
    public string Name { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public LightLocation Location { get; set; } = LightLocation.NONE;
    public string MqttTopic { get; set; } = string.Empty;
    public bool IsOn { get; set; }
    public int Brightness { get; set; }
    public string Color { get; set; } = string.Empty;
    public int ColorTemperature { get; set; }
    public DateTime LastStatusChange { get; set; } = DateTime.MinValue;
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.MinValue;
}
