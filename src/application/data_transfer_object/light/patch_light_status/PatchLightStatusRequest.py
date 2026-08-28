from pydantic.dataclasses import dataclass
from domain.model.enum.light.LightLocation import LightLocation

@dataclass
class PatchLightStatusRequest:
    lightLocation: LightLocation
    isOn: bool