from datetime import datetime
from pydantic.dataclasses import dataclass

@dataclass
class CreateRainSensorRequestJson:
    callOut: str = ""
    deviceName: str = ""
    deviceType: str = ""
    model: str = ""
    manufacturer: str = ""
    macAddress: str = ""
    adcValue: int = 4095
    wetnessPercent: int = 0
    isRaining: bool = False
    measureAt: datetime = datetime.min
