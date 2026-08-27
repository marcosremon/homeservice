from pydantic.dataclasses import dataclass

@dataclass
class CreateTemperatureSensorRequestJson:
    callOut: str = ""
    deviceName: str = ""
    deviceType: str = ""
    model: str = ""
    manufacturer: str = ""
    macAddress: str = ""
    temperature: float = 0.0
    adcVoltage: float = 0.0
    measureAt: float = 0.0
