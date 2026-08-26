from pydantic.dataclasses import dataclass

@dataclass
class PatchPresenceSensorDataRequest:
    callout: str = ""
    device_name: str = ""
    device_type: str = ""
    model: str = ""
    manufacturer: str = ""
    mac_address: str = ""
    ts: int = 0
    presence: bool = False
    distance_cm: int = 0
    motion: str = ""