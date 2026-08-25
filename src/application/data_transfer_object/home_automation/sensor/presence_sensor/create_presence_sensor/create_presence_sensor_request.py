from dataclasses import dataclass
from datetime import datetime


@dataclass
class CreatePresenceSensorRequest:
    call_out: str = ""
    device_name: str = ""
    device_type: str = ""
    ts: int = 0
    presence: bool = False
    distance_cm: int = 0
    motion: str = ""
    last_detected_presence: datetime = datetime.min
    model: str = ""
    manufacturer: str = ""
    mac_address: str = ""
