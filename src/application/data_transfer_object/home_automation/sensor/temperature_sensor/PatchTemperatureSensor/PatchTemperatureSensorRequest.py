from dataclasses import dataclass

@dataclass
class PatchTemperatureSensorRequest:
    callOut: str = ""
    deviceName: str = ""
    deviceType: str = ""
    model: str = ""
    macAddress: str = ""
    temperature: float | None = None
    adcVoltage: float | None = None
    measureAt: float | None = None