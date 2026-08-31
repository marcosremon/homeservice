
from pydantic.dataclasses import dataclass

@dataclass
class CreatePresenceSensorRequestJson:
    callout: str = ""
    deviceName: str = ""
    deviceType: str = ""
    ts: int = 0
    presence: bool = False
    distanceCm: int = 0
    motion: str = ""
    model: str = ""
    manufacturer: str = ""
    macAddress: str = ""