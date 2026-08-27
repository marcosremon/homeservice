from pydantic.dataclasses import dataclass
from domain.model.enum.Light.LightLocation import LightLocation

@dataclass
class PatchLightStatusRequest:
    lightLocation: LightLocation
    isOn: bool