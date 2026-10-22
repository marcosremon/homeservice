using HomeService.Domain.Model.Enum.Light;

namespace HomeService.Domain.Model.Entity;

public class Light
{
    public long LightId { get; set; }
    public long DeviceId { get; set; }

    // se guarda el nombre del enum, no su valor numerico
    public string Location { get; set; } = nameof(LightLocation.NONE);

    public string MqttTopic { get; set; } = string.Empty;
    public bool IsOn { get; set; }
    public int Brightness { get; set; }
    public string Color { get; set; } = string.Empty;
    public int ColorTemperature { get; set; }
    public DateTime LastStatusChange { get; set; } = DateTime.MinValue;
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.MinValue;
}
