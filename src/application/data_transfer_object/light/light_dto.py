from dataclasses import dataclass
from datetime import datetime

from domain.model.enum.Light.light_location import LightLocation

@dataclass
class LightDto:
    name: str = ""
    room: str = ""
    location: LightLocation = LightLocation.NONE
    mqtt_topic: str = ""
    is_on: bool = False
    brightness: int = 0
    color: str = ""
    color_temperature: int = 0
    last_status_change: datetime = datetime.min
    is_online: bool = False
    last_seen: datetime = datetime.min