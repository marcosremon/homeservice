using HomeService.Domain.Model.Enum.Roomba;

namespace HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;

public class PatchRoombaStateRequest
{
    public bool IsActivation { get; set; }
    public bool IsFinished { get; set; }
    public RoombaTarget Target { get; set; } = RoombaTarget.FULL_HOUSE;
    public RoombaPhase Phase { get; set; } = RoombaPhase.STOP;
    public int BatteryPercent { get; set; }
    public bool BinFull { get; set; }
    public int ErrorCode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string PmapId { get; set; } = string.Empty;
    public string UserPmapvId { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
}
