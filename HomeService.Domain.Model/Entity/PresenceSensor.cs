namespace HomeService.Domain.Model.Entity;

public class PresenceSensor
{
    public long PresenceSensorId { get; set; }
    public long DeviceId { get; set; }
    public long Ts { get; set; }
    public bool Presence { get; set; }
    public int DistanceCm { get; set; }
    public string Motion { get; set; } = string.Empty;
    public DateTime LastDetectedPresence { get; set; } = DateTime.MinValue;
}
