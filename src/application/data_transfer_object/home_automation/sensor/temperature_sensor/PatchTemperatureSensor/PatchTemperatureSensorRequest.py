from dataclasses import dataclass

@dataclass
class PatchTemperatureSensorRequest:
    callOut: str = ""
    deviceName: str = ""
    deviceType: str = ""
    model: str = ""
    macAddress: str = ""
    temperature: float = 0.0
    adcVoltage: float = 0.0
    measureAt: float = 0.0