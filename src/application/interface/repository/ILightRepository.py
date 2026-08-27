from abc import ABC, abstractmethod

from application.data_transfer_object.light.get_light_by_location.GetLightByLocationRequest import GetLightByLocationRequest
from application.data_transfer_object.light.get_light_by_location.GetLightByLocationResponse import GetLightByLocationResponse
from application.data_transfer_object.light.patch_light_status.PatchLightStatusRequest import PatchLightStatusRequest
from domain.model.enum.Light.LightLocation import LightLocation

class ILightRepository(ABC):
    @abstractmethod
    async def GetLightByLocation(self, getLightByLocationRequest: GetLightByLocationRequest) -> GetLightByLocationResponse: ...

    @abstractmethod
    async def PatchLightStatus(self, patchLightStatusRequest: PatchLightStatusRequest) -> None: ...