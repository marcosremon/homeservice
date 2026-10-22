using HomeService.Domain.Model.Enum.Roomba;

namespace HomeService.Domain.Model.Entity;

public class Roomba
{
    public long RoombaId { get; set; }
    public long DeviceId { get; set; }

    // se guarda el nombre del enum, no su valor numerico
    public string Phase { get; set; } = nameof(RoombaPhase.STOP);

    public int BatteryPercent { get; set; }
    public bool BinFull { get; set; }
    public string LastTarget { get; set; } = string.Empty;
    public DateTime LastRoombaActivation { get; set; } = DateTime.MinValue;
    public DateTime LastRoombaEnd { get; set; } = DateTime.MinValue;
    public int LastCleanDurationMinutes { get; set; }
    public int ErrorCode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string PmapId { get; set; } = string.Empty;
    public string UserPmapvId { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.MinValue;
}
