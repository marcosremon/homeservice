from dataclasses import dataclass

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
