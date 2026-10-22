using HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;
using HomeService.Domain.Model.Enum.Roomba;

namespace HomeService.Transversal.Common.Roomba;

public class RoombaState
{
    public string PmapId { get; set; } = string.Empty;
    public string PmapVersion { get; set; } = string.Empty;
    public int Battery { get; set; }
    public bool BinFull { get; set; }
    public int ErrorCode { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public RoombaPhase Phase { get; set; } = RoombaPhase.STOP;
    public bool PhaseSeen { get; set; }
    public bool CommandAccepted { get; set; }
    public bool FinishSeen { get; set; }
    public List<PatchRoombaStateRequest> ActivationEvents { get; set; } = [];
}
