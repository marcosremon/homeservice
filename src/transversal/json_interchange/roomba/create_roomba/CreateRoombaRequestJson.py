from pydantic.dataclasses import dataclass

@dataclass
class CreateRoombaRequestJson:
    callout: str = ""
    deviceName: str = ""
    deviceType: str = ""
    model: str = ""
    manufacturer: str = ""
    macAddress: str = ""