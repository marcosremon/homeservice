import asyncio
from datetime import timedelta
from application.data_transfer_object.alexa.AlexaRequest import AlexaRequest
from application.interface.service.IRoombaService import IRoombaService
from domain.model.enum.Roomba.RoombaTarget import RoombaTarget
from infraestructure.gateway.roomba.RoombaUtils import RoombaUtils
from transversal.common.alexa.alexa_request.AlexaSlot import AlexaSlot
from transversal.common.utils.AlexaUtils import AlexaUtils
from transversal.common.utils.GeneralUtils import GeneralUtils

class RoombaService(IRoombaService):

    def __init__(self) -> None:
        self._pendingTimers: set[asyncio.Task[None]] = set()

    # region ExecuteRoombaOrder
    async def ExecuteRoombaOrder(self, intentName: str, alexaRequest: AlexaRequest) -> str:
        match intentName:
            case "roomba_order_limpiar_cocina": return await self._startRoomba(RoombaTarget.KITCHEN)
            case "roomba_order_limpiar_cuarto_diego": return await self._startRoomba(RoombaTarget.DIEGO)
            case "roomba_order_limpiar_cuarto_marcos": return await self._startRoomba(RoombaTarget.MARCOS)
            case "roomba_order_limpiar_cuarto_abuela_y_cocina": return await self._startRoomba(RoombaTarget.KITCHEN_AND_GRANDMOTHER)
            case "roomba_order_limpiar_cuarto_diego_y_cuarto_marcos": return await self._startRoomba(RoombaTarget.BEDROOMS)
            case "roomba_order_limpiar_banyos_y_cuarto_padres": return await self._startRoomba(RoombaTarget.BEDROOM_AND_TOILET)
            case "roomba_order_limpiar_pasillos_y_banyo": return await self._startRoomba(RoombaTarget.HALLWAY_AND_TOILET)
            case "roomba_order_limpiar_comedor": return await self._startRoomba(RoombaTarget.LIVING_ROOM)
            case "roomba_order_limpiar_casa_completa": return await self._startRoomba(RoombaTarget.FULL_HOUSE)

            case "roomba_order_limpiar_casa_en_x_tiempo": return self._startRoombaWithTimerBackground(alexaRequest, RoombaTarget.FULL_HOUSE)
            case "roomba_order_limpiar_cocina_en_x_tiempo": return self._startRoombaWithTimerBackground(alexaRequest, RoombaTarget.KITCHEN)
            case "roomba_order_limpiar_comedor_en_x_tiempo": return self._startRoombaWithTimerBackground(alexaRequest, RoombaTarget.LIVING_ROOM)
            case "roomba_order_limpiar_cuarto_diego_en_x_tiempo": return self._startRoombaWithTimerBackground(alexaRequest, RoombaTarget.DIEGO)
            case "roomba_order_limpiar_cuarto_marcos_en_x_tiempo": return self._startRoombaWithTimerBackground(alexaRequest, RoombaTarget.MARCOS)
            case "roomba_order_limpiar_cuarto_abuela_y_cocina_en_x_tiempo": return self._startRoombaWithTimerBackground(alexaRequest, RoombaTarget.KITCHEN_AND_GRANDMOTHER)
            case "roomba_order_limpiar_cuarto_diego_y_cuarto_marcos_en_x_tiempo": return self._startRoombaWithTimerBackground(alexaRequest, RoombaTarget.BEDROOMS)
            case "roomba_order_limpiar_banyos_y_cuarto_padres_en_x_tiempo": return self._startRoombaWithTimerBackground(alexaRequest, RoombaTarget.BEDROOM_AND_TOILET)
            case "roomba_order_limpiar_pasillos_y_banyo_en_x_tiempo": return self._startRoombaWithTimerBackground(alexaRequest, RoombaTarget.HALLWAY_AND_TOILET)

            case "roomba_order_pausar_roomba": return await self._pauseRoomba()
            case "roomba_order_enviar_roomba_a_casa": return await self._sendRoombaHome()

            case _: return "Orden no reconocida"
    # endregion

    # region _startRoombaWithTimerBackground
    def _startRoombaWithTimerBackground(self, alexaRequest: AlexaRequest, target: RoombaTarget) -> str:
        alexaSlots: dict[str, AlexaSlot] | None = alexaRequest.alexaRequestData.intent.slots if alexaRequest.alexaRequestData.intent is not None else None
        if not alexaSlots:
            return "No se encontro el tiempo indicado."

        # Alexa manda la duracion en uno de estos tres slots segun el intent.
        slot: AlexaSlot | None = alexaSlots.get("duracion")
        if slot is None or slot.value is None:
            slot = alexaSlots.get("tiempo")

        if slot is None or slot.value is None:
            slot = alexaSlots.get("time")

        time: str = slot.value if slot is not None and slot.value is not None else ""

        if GeneralUtils.IsNullOrEmpty(time):
            return "No se encontro el tiempo indicado."

        timer: asyncio.Task[None] = asyncio.create_task(self._startRoombaAfter(time, target))
        self._pendingTimers.add(timer)
        timer.add_done_callback(self._pendingTimers.discard)

        return f"De acuerdo, limpiare en {time}."
    # endregion

    # region _startRoombaAfter
    @staticmethod
    async def _startRoombaAfter(time: str, target: RoombaTarget) -> None:
        try:
            duration: timedelta = AlexaUtils.ParseAlexaDuration(time)

            await asyncio.sleep(duration.total_seconds())
            await RoombaUtils.StartRoomba(target)
        except asyncio.CancelledError:
            raise
        except Exception as ex:
            print(f"[Timer] Error en background: {ex}")
    # endregion

    # region _startRoomba
    @staticmethod
    async def _startRoomba(target: RoombaTarget) -> str:
        return await RoombaUtils.StartRoomba(target)
    # endregion

    # region _pauseRoomba
    @staticmethod
    async def _pauseRoomba() -> str:
        return await RoombaUtils.PauseRoomba()
    # endregion

    # region _sendRoombaHome
    @staticmethod
    async def _sendRoombaHome() -> str:
        return await RoombaUtils.SendRoombaHome()
    # endregion