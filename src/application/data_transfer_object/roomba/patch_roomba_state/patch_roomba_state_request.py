from dataclasses import dataclass, field
from datetime import datetime, timezone

from domain.model.enum.Roomba.roomba_phase import RoombaPhase
from domain.model.enum.Roomba.roomba_target import RoombaTarget

@dataclass
class PatchRoombaStateRequest:
    event_time: datetime = field(default_factory = lambda: datetime.now(timezone.utc))
    is_activation: bool = False
    is_finished: bool = False
    target: RoombaTarget = RoombaTarget.FULL_HOUSE
    phase: RoombaPhase = RoombaPhase.STOP
    battery_percent: int = 0
    bin_full: bool = False
    error_code: int = 0
    error_message: str = ""
    pmap_id: str = ""
    user_pmapv_id: str = ""
    is_online: bool = False