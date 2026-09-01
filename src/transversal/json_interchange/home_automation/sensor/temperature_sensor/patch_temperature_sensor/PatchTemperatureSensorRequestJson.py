from pydantic.dataclasses import dataclass

@dataclass
class PatchTemperatureSensorRequestJson:
    callOut: str = ""
    deviceName: str = ""
    deviceType: str = ""
    model: str = ""
    macAddress: str = ""
    temperature: float | None = None
    adcVoltage: float | None = None
    measureAt: float | None = None
