from dataclasses import dataclass

@dataclass
class PatchTemperatureSensor:
    callOut: str = ""
    deviceName: str = ""
    deviceType: str = ""
    model: str = ""
    macAddress: str = ""
    temperature: float = 0.0
    adcVoltage: float = 0.0
    measureAt: float = 0.0