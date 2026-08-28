from dataclasses import dataclass

from domain.model.enum.light.LightLocation import LightLocation

@dataclass
class GetLightByLocationRequest:
    location: LightLocation = LightLocation.NONE