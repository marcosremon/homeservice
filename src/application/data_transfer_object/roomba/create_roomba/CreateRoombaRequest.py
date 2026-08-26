from dataclasses import dataclass

@dataclass
class CreateRoombaRequest:
    callout: str = ""
    deviceName: str = ""
    deviceType: str = ""
    model: str = ""
    manufacturer: str = ""
    macAddress: str = ""