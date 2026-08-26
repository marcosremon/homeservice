from datetime import datetime

from pydantic.dataclasses import dataclass

@dataclass
class PatchRoombaStateRequestJson:
    # datetime.min hace de "default" de C#: el controlador lo sustituye por la
    # hora actual, igual que el `== default` del RoombaController.
    eventTime: datetime = datetime.min
    isActivation: bool = False
    isFinished: bool = False
    target: str = ""
    phase: str = ""
    batteryPercent: int = 0
    binFull: bool = False
    errorCode: int = 0
    errorMessage: str = ""
    pmapId: str = ""
    userPmapvId: str = ""
    isOnline: bool = False