from dataclasses import dataclass
from datetime import datetime

@dataclass
class CreatePresenceSensorRequest:
    callout: str = ""
    deviceName: str = ""
    deviceType: str = ""
    ts: int = 0
    presence: bool = False
    distanceCm: int = 0
    motion: str = ""
    lastDetectedPresence: datetime = datetime.min
    model: str = ""
    manufacturer: str = ""
    macAddress: str = ""