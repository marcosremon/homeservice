from dataclasses import dataclass, field
from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateRequest import PatchRoombaStateRequest
from domain.model.enum.Roomba.RoombaPhase import RoombaPhase

@dataclass
class RoombaState:
    pmapId: str = ""
    pmapVersion: str = ""
    battery: int = 0
    binFull: bool = False
    errorCode: int = 0
    errorMessage: str = ""
    phase: RoombaPhase = RoombaPhase.STOP
    phaseSeen: bool = False
    commandAccepted: bool = False
    activationEvents: list[PatchRoombaStateRequest] = field(default_factory = list)