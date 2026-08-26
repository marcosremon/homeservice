from dataclasses import dataclass

from domain.model.enum.Light.LightLocation import LightLocation

@dataclass
class GetLightByLocationRequest:
    location: LightLocation = LightLocation.NONE