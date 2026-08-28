from application.data_transfer_object.alexa.AlexaRequest import AlexaRequest
from application.interface.service.IMqttService import IMqttService
from application.interface.service.ITemperatureSensorService import ITemperatureSensorService
from domain.model.enum.temperature.TemperatureMode import TemperatureMode
from domain.model.enum.temperature.ThermostatTarget import ThermostatTarget
from transversal.common.alexa.alexa_request.AlexaSlot import AlexaSlot
from transversal.common.configuration.Settings import Settings
from transversal.common.utils.GeneralUtils import GeneralUtils

class TemperatureSensorService(ITemperatureSensorService):

    def __init__(self, mqttService: IMqttService, settings: Settings):
        self._mqttService: IMqttService = mqttService
        self._settings: Settings = settings

    # region ExecuteTemperatureSensorOrder
    async def ExecuteTemperatureSensorOrder(self, intentName: str, alexaRequest: AlexaRequest) -> str:
        degrees: str = self._getSlot(alexaRequest, "grados")
        room: str = self._getSlot(alexaRequest, "habitacion")

        parametersDegrees: list[str] = [degrees]
        parametersRoom: list[str] = [room]
        parametersRoomAndDegrees: list[str] = [degrees, room]

        match intentName:
            case "temperature_sensor_order_subir_temperatura": return await self._temperatureManager(TemperatureMode.INCREASE)
            case "temperature_sensor_order_subir_temperatura_en_x_habitacion": return await self._temperatureManager(TemperatureMode.INCREASE, parametersRoom)
            case "temperature_sensor_order_subir_x_temperatura": return await self._temperatureManager(TemperatureMode.INCREASE, parametersDegrees)
            case "temperature_sensor_order_subir_x_temperatura_en_x_habitacion": return await self._temperatureManager(TemperatureMode.INCREASE, parametersRoomAndDegrees)

            case "temperature_sensor_order_bajar_temperatura": return await self._temperatureManager(TemperatureMode.DECREASE)
            case "temperature_sensor_order_bajar_temperatura_en_x_habitacion": return await self._temperatureManager(TemperatureMode.DECREASE, parametersRoom)
            case "temperature_sensor_order_bajar_x_temperatura": return await self._temperatureManager(TemperatureMode.DECREASE, parametersDegrees)
            case "temperature_sensor_order_bajar_x_temperatura_en_x_habitacion": return await self._temperatureManager(TemperatureMode.DECREASE, parametersRoomAndDegrees)

            case _: return "Orden no reconocida"
    # endregion

    # region _temperatureManager
    async def _temperatureManager(self, temperatureMode: TemperatureMode, parameters: list[str] | None = None) -> str:
        degrees: int = 2
        thermostatTarget: ThermostatTarget = ThermostatTarget.FULL_HOUSE

        if parameters:
            for parameter in parameters:
                if GeneralUtils.IsNullOrEmpty(parameter):
                    continue

                if parameter.isdigit():
                    degrees = int(parameter)
                else:
                    thermostatTarget = self._parseThermostatTarget(parameter)

        isSuccess: bool = await self._changeThermostatTemperature(temperatureMode, degrees, thermostatTarget)

        if not isSuccess:
            return "Hubo un error al cambiar la temperatura"

        action: str = "Subiendo" if temperatureMode == TemperatureMode.INCREASE else "Bajando"
        return f"{action} {degrees} grados en {thermostatTarget.name}"
    # endregion

    # region _changeThermostatTemperature
    async def _changeThermostatTemperature(self, temperatureMode: TemperatureMode, degrees: int, thermostatTarget: ThermostatTarget) -> bool:
        topic: str = f"home/{thermostatTarget.name.lower()}/thermostat/cmd"

        # el termostato aplica el delta el mismo: aqui no guardamos la consigna actual.
        sign: str = "+" if temperatureMode == TemperatureMode.INCREASE else "-"
        payload: str = f"{sign}{degrees}"

        try:
            # sin retain: un delta retenido se reaplicaria en cada reconexion.
            await self._mqttService.Publish(topic, payload, retain = False)
        except Exception as ex:
            print(f"TemperatureSensorService -> _changeThermostatTemperature -> {ex}")

            return False

        return True
    # endregion

    # region _parseThermostatTarget
    @staticmethod
    def _parseThermostatTarget(room: str) -> ThermostatTarget:
        normalizedRoom: str = GeneralUtils.RemoveAccents(room.strip().lower())

        match normalizedRoom:
            case "cocina": return ThermostatTarget.KITCHEN
            case "cuarto de diego" | "habitacion de diego" | "diego": return ThermostatTarget.DIEGO
            case "cuarto de marcos" | "habitacion de marcos" | "marcos": return ThermostatTarget.MARCOS
            case "cocina y abuela" | "cocina y cuarto de la abuela" | "abuela": return ThermostatTarget.KITCHEN_AND_GRANDMOTHER
            case "cuartos" | "habitaciones" | "dormitorios": return ThermostatTarget.BEDROOMS
            case "cuarto de los padres y baño" | "cuarto de padres y baño": return ThermostatTarget.BEDROOM_AND_TOILET
            case "pasillos y baño" | "pasillo y baño": return ThermostatTarget.HALLWAY_AND_TOILET
            case "salon" | "comedor": return ThermostatTarget.LIVING_ROOM
            case "casa" | "toda la casa" | "casa entera": return ThermostatTarget.FULL_HOUSE

            case _: return ThermostatTarget.FULL_HOUSE
    # endregion

    # region _getSlot
    @staticmethod
    def _getSlot(alexaRequest: AlexaRequest, slotName: str) -> str:
        alexaSlots: dict[str, AlexaSlot] | None = alexaRequest.alexaRequestData.intent.slots if alexaRequest.alexaRequestData.intent is not None else None
        if not alexaSlots:
            return ""

        slot: AlexaSlot | None = alexaSlots.get(slotName)
        if slot is None or slot.value is None:
            return ""

        return slot.value
    # endregion
