from dataclasses import dataclass

from domain.model.enum.roomba.RoombaPhase import RoombaPhase
from domain.model.enum.roomba.RoombaTarget import RoombaTarget

@dataclass
class PatchRoombaStateRequest:
    isActivation: bool = False
    isFinished: bool = False
    target: RoombaTarget = RoombaTarget.FULL_HOUSE
    phase: RoombaPhase = RoombaPhase.STOP
    batteryPercent: int = 0
    binFull: bool = False
    errorCode: int = 0
    errorMessage: str = ""
    pmapId: str = ""
    userPmapvId: str = ""
    isOnline: bool = False