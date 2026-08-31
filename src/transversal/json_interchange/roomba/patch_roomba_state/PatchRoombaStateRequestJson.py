from pydantic.dataclasses import dataclass

@dataclass
class PatchRoombaStateRequestJson:
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