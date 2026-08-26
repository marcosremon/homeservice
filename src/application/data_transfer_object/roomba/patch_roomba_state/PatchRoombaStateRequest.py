from dataclasses import dataclass, field
from datetime import datetime, timezone

from domain.model.enum.Roomba.RoombaPhase import RoombaPhase
from domain.model.enum.Roomba.RoombaTarget import RoombaTarget

@dataclass
class PatchRoombaStateRequest:
    eventTime: datetime = field(default_factory = lambda: datetime.now(timezone.utc))
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