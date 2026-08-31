from pydantic.dataclasses import dataclass

@dataclass
class PatchRainSensorRequestJson:
    callOut: str = ""
    deviceName: str = ""
    deviceType: str = ""
    model: str = ""
    macAddress: str = ""
    adcValue: int = 4095
    wetnessPercent: int = 0
    isRaining: bool = False
