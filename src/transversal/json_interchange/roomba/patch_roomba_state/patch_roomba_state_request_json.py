from datetime import datetime

from pydantic.dataclasses import dataclass

@dataclass
class PatchRoombaStateRequestJson:
    # datetime.min hace de "default" de C#: el controlador lo sustituye por la
    # hora actual, igual que el `== default` del RoombaController.
    event_time: datetime = datetime.min
    is_activation: bool = False
    is_finished: bool = False
    target: str = ""
    phase: str = ""
    battery_percent: int = 0
    bin_full: bool = False
    error_code: int = 0
    error_message: str = ""
    pmap_id: str = ""
    user_pmapv_id: str = ""
    is_online: bool = False
