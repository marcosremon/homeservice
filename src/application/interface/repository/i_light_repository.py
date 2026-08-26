from abc import ABC, abstractmethod

from application.data_transfer_object.light.get_light_by_location.get_light_by_location_request import GetLightByLocationRequest
from application.data_transfer_object.light.get_light_by_location.get_light_by_location_response import GetLightByLocationResponse
from domain.model.enum.Light.light_location import LightLocation

class ILightRepository(ABC):
    @abstractmethod
    async def get_light_by_location(self, get_light_by_location_request: GetLightByLocationRequest) -> GetLightByLocationResponse: ...

    @abstractmethod
    async def patch_light_status(self, light_location: LightLocation, is_on: bool) -> None: ...