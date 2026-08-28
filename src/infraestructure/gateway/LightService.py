from application.data_transfer_object.light.LightDto import LightDto
from application.data_transfer_object.light.get_light_by_location.GetLightByLocationRequest import GetLightByLocationRequest
from application.data_transfer_object.light.get_light_by_location.GetLightByLocationResponse import GetLightByLocationResponse
from application.data_transfer_object.light.patch_light_status.PatchLightStatusRequest import PatchLightStatusRequest
from application.interface.repository.ILightRepository import ILightRepository
from application.interface.service.ILightService import ILightService
from application.interface.service.IMqttService import IMqttService
from domain.model.enum.light.LightLocation import LightLocation
from domain.model.enum.light.LightStatus import LightStatus
from transversal.common.utils.GeneralUtils import GeneralUtils

class LightService(ILightService):

    def __init__(self, mqttService: IMqttService, lightRepository: ILightRepository):
        self._mqttService: IMqttService = mqttService
        self._lightRepository: ILightRepository = lightRepository

    # region ExecuteLightOrder
    async def ExecuteLightOrder(self, intentName: str) -> str:
        match intentName:
            case "light_order_encender_luces_salon": return await self._lightManager(LightLocation.LIVING_ROOM, LightStatus.ON)

            case "light_order_apagar_luces_salon": return await self._lightManager(LightLocation.LIVING_ROOM, LightStatus.OFF)

            case _: return "Orden no reconocida"
    # endregion

    # region _lightManager
    async def _lightManager(self, lightLocation: LightLocation, lightStatus: LightStatus) -> str:
        lightDto: LightDto | None = await self._getLight(lightLocation)
        if lightDto is None:
            return f"No encuentro la luz de {lightLocation.name}"

        isOn: bool = lightDto.isOn

        if lightStatus == LightStatus.ON and isOn:
            return f"La luz de {lightLocation.name} ya esta encendida"

        if lightStatus == LightStatus.OFF and not isOn:
            return f"La luz de {lightLocation.name} ya esta apagada"

        await self._changeLightStatus(lightDto, lightStatus)

        return f"Encendiendo la luz de {lightLocation.name}" if lightStatus == LightStatus.ON else f"Apagando la luz de {lightLocation.name}"
    # endregion

    # region _changeLightStatus
    async def _changeLightStatus(self, lightDto: LightDto, lightStatus: LightStatus) -> None:
        topic: str = lightDto.mqttTopic if not GeneralUtils.IsNullOrEmpty(lightDto.mqttTopic) else f"home/{lightDto.location.name.lower()}/lights/cmd"
        status: str = lightStatus.name

        await self._mqttService.Publish(topic, status)

        patchLightStatusRequest: PatchLightStatusRequest = PatchLightStatusRequest(
            lightLocation = lightDto.location,
            isOn = lightDto.isOn,
        )
        await self._lightRepository.PatchLightStatus(patchLightStatusRequest)
    # endregion

    # region _getLight
    async def _getLight(self, lightLocation: LightLocation) -> LightDto | None:
        getLightByLocationRequest: GetLightByLocationRequest = GetLightByLocationRequest(
            location = lightLocation,
        )

        getLightByLocationResponse: GetLightByLocationResponse = await self._lightRepository.GetLightByLocation(getLightByLocationRequest)

        return getLightByLocationResponse.lightDto if getLightByLocationResponse.isSuccess else None
    # endregion