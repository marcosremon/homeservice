from dataclasses import dataclass

from domain.model.enum.Light.light_location import LightLocation

@dataclass
class GetLightByLocationRequest:
    location: LightLocation = LightLocation.NONE