from pydantic.dataclasses import dataclass

@dataclass
class CreateRoombaRequestJson:
    callout: str = ""
    device_name: str = ""
    device_type: str = ""
    model: str = ""
    manufacturer: str = ""
    mac_address: str = ""