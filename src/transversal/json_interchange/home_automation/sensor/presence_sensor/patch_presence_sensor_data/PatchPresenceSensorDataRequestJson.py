from pydantic.dataclasses import dataclass

@dataclass
class PatchPresenceSensorDataRequestJson:
    callout: str = ""
    deviceName: str = ""
    deviceType: str = ""
    model: str = ""
    manufacturer: str = ""
    macAddress: str = ""
    ts: int = 0
    presence: bool = False
    distanceCm: int = 0
    motion: str = ""