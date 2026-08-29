from dataclasses import dataclass
from datetime import datetime

@dataclass
class PatchRainSensorRequest:
    callOut: str = ""
    deviceName: str = ""
    deviceType: str = ""
    model: str = ""
    macAddress: str = ""
    adcValue: int = 4095
    wetnessPercent: int = 0
    isRaining: bool = False
    measureAt: datetime = datetime.min
