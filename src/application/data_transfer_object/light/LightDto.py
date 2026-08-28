from dataclasses import dataclass
from datetime import datetime

from domain.model.enum.light.LightLocation import LightLocation

@dataclass
class LightDto:
    name: str = ""
    room: str = ""
    location: LightLocation = LightLocation.NONE
    mqttTopic: str = ""
    isOn: bool = False
    brightness: int = 0
    color: str = ""
    colorTemperature: int = 0
    lastStatusChange: datetime = datetime.min
    isOnline: bool = False
    lastSeen: datetime = datetime.min