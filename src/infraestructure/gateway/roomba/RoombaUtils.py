import asyncio
from datetime import datetime, time, timezone

from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateRequest import PatchRoombaStateRequest
from domain.model.enum.Roomba.RoombaAction import RoombaAction
from domain.model.enum.Roomba.RoombaPhase import RoombaPhase
from domain.model.enum.Roomba.RoombaTarget import RoombaTarget
from infraestructure.gateway.roomba.payload.RoombaPayload import RoombaPayload
from infraestructure.gateway.roomba.payload.RoombaRegion import RoombaRegion
from infraestructure.gateway.roomba.payload.RoombaRegionParams import RoombaRegionParams
from transversal.common.configuration.Settings import Settings, GetSettings
from transversal.common.utils.GeneralUtils import GeneralUtils

# Equivalente a SemaphoreSlim(1, 1): la Roomba no admite dos ordenes a la vez.
_roombaLock: asyncio.Lock = asyncio.Lock()

# Ventana en la que se permite el arranque automatico.
_AUTO_START_FROM: time = time(8, 0)
_AUTO_START_TO: time = time(21, 0)

# Espera maxima a que la Roomba confirme la orden (phase: run).
_COMMAND_CONFIRMATION_TIMEOUT_SECONDS: float = 8.0

# Pausa entre PAUSE y DOCK al mandarla a casa.
_PAUSE_BEFORE_DOCK_SECONDS: float = 3.0

_ROOM_IDS_BY_TARGET: dict[RoombaTarget, list[str]] = {
    RoombaTarget.KITCHEN: ["11"],
    RoombaTarget.DIEGO: ["16"],
    RoombaTarget.MARCOS: ["21"],
    RoombaTarget.KITCHEN_AND_GRANDMOTHER: ["4", "11"],
    RoombaTarget.BEDROOMS: ["21", "16"],
    RoombaTarget.BEDROOM_AND_TOILET: ["23", "25"],
    RoombaTarget.HALLWAY_AND_TOILET: ["19", "22", "24"],
    RoombaTarget.LIVING_ROOM: ["1", "18"],
    RoombaTarget.FULL_HOUSE: [],
}

_PHASES_BY_NAME: dict[str, RoombaPhase] = {
    "charge": RoombaPhase.CHARGE,
    "run": RoombaPhase.RUN,
    "stop": RoombaPhase.STOP,
    "hmUsrDock": RoombaPhase.HM_USR_DOCK,
    "stuck": RoombaPhase.STUCK,
}

# region get_room_info
async def GetRoomInfo() -> None:
    """Vuelca por consola el mapa de habitaciones que conoce la Roomba.

    Solo sirve para averiguar los region_id a mano cuando cambias el mapa.
    """
    # TODO (MQTT): suscribirse a $aws/things/{blid}/shadow/update y volcar el
    # nodo pmaps del mensaje recibido.
    print("RoombaUtils -> get_room_info -> pendiente del transporte MQTT.")
# endregion

# region get_roomba_phase
async def GetRoombaPhase() -> RoombaPhase | None:
    """Fase actual de la Roomba, o None si no se puede consultar."""
    # TODO (MQTT): conectar, escuchar cleanMissionStatus.phase y devolver
    # _parse_phase(phase). None significa "no se pudo consultar", y todas las
    # funciones de abajo ya lo tratan como tal.
    return None
# endregion

# region send_roomba_order
async def SendRoombaOrder(roombaAction: RoombaAction, roombaTarget: RoombaTarget | None = None) -> None:
    """Publica la orden en cmd/{blid}/delta.

    En C# son dos sobrecargas (con y sin RoombaTarget); en Python el target es
    opcional, que es lo mismo con una sola funcion.
    """
    settings: Settings = GetSettings()

    if (GeneralUtils.IsNullOrEmpty(settings.roombaId) or
        GeneralUtils.IsNullOrEmpty(settings.roombaBlid) or
        GeneralUtils.IsNullOrEmpty(settings.roombaPasswd)
    ):
        print("RoombaUtils -> send_roomba_order -> Configuracion incompleta (ip, blid o pass vacios)")
        return

    roomIds: list[str] = _getRoombaRoomsIds(roombaTarget) if roombaTarget is not None else []
    payload: RoombaPayload = _buildPayload(roombaAction, roomIds, settings)

    # TODO (MQTT): conectar con tls a {roomba_id}:{roomba_port} con credenciales
    # (blid, passwd), publicar `payload` en cmd/{blid}/delta y escuchar el shadow
    # hasta que phase == "run" o pasen _COMMAND_CONFIRMATION_TIMEOUT_SECONDS.
    # Cuando llegue esa confirmacion hay que publicar el evento de activacion:
    #     RoombaActivatedEvent.publish(build_activation_request(...))
    # y al terminar la espera, el evento de estado final.
    print(f"RoombaUtils -> send_roomba_order -> pendiente del transporte MQTT. Payload: {payload}")
# endregion

# region build_activation_request
def BuildActivationRequest(
    roombaPhase: RoombaPhase,
    roombaTarget: RoombaTarget | None = None,
    isActivation: bool = False,
    batteryPercent: int = 0,
    binFull: bool = False,
    errorCode: int = 0,
    errorMessage: str = "",
    pmapId: str = "",
    userPmapvId: str = "",
) -> PatchRoombaStateRequest:
    return PatchRoombaStateRequest(
        eventTime = datetime.now(timezone.utc),
        isActivation = isActivation,
        target = roombaTarget if roombaTarget is not None else RoombaTarget.FULL_HOUSE,
        phase = roombaPhase,
        batteryPercent = batteryPercent,
        binFull = binFull,
        errorCode = errorCode,
        errorMessage = errorMessage,
        pmapId = pmapId,
        userPmapvId = userPmapvId,
        isOnline = True,
    )
# endregion

# region start_roomba_if_house_is_empty
async def StartRoombaIfHouseIsEmpty(lastRoombaActivation: datetime) -> None:
    async with _roombaLock:
        try:
            now: datetime = datetime.now()

            isValidTime: bool = _AUTO_START_FROM <= now.time() <= _AUTO_START_TO
            isActivatedToday: bool = lastRoombaActivation.date() == now.date()

            if isActivatedToday:
                print("Roomba ya se activo hoy, omitiendo.")
                return

            if not isValidTime:
                print(f"Fuera de horario ({_AUTO_START_FROM:%H:%M} - {_AUTO_START_TO:%H:%M}), omitiendo.")
                return

            roombaPhase: RoombaPhase | None = await GetRoombaPhase()
            if roombaPhase is None:
                print("No se pudo obtener el estado del Roomba, abortando.")
                return

            if roombaPhase == RoombaPhase.STUCK:
                print("Roomba atascado, revisalo manualmente.")
                return

            if roombaPhase in (RoombaPhase.RUN, RoombaPhase.HM_USR_DOCK):
                print(f"start_roomba_if_house_is_empty ignorado - fase actual: {roombaPhase.name}")
                return

            await SendRoombaOrder(RoombaAction.START, RoombaTarget.FULL_HOUSE)
        except Exception as ex:
            print(f"Error en RoombaUtils -> start_roomba_if_house_is_empty: {ex}")
# endregion

# region start_roomba
async def StartRoomba(roombaTarget: RoombaTarget) -> str:
    async with _roombaLock:
        try:
            roombaPhase: RoombaPhase | None = await GetRoombaPhase()
            if roombaPhase is None:
                return "No se pudo obtener el estado del Roomba."

            if roombaPhase == RoombaPhase.STUCK:
                return "El Roomba esta atascado, revisalo manualmente."

            if roombaPhase in (RoombaPhase.RUN, RoombaPhase.HM_USR_DOCK):
                return "El Roomba ya esta limpiando."

            await SendRoombaOrder(RoombaAction.START, roombaTarget)

            if roombaTarget == RoombaTarget.FULL_HOUSE:
                return "Iniciando limpieza de la casa completa."

            return f"Iniciando limpieza de {roombaTarget.name.lower().replace('_', ' ')}."
        except Exception as ex:
            return f"Error al iniciar el Roomba: {ex}"
# endregion

# region send_roomba_home
async def SendRoombaHome() -> str:
    async with _roombaLock:
        try:
            roombaPhase: RoombaPhase | None = await GetRoombaPhase()
            if roombaPhase is None:
                return "No se pudo obtener el estado del Roomba."

            if roombaPhase == RoombaPhase.STUCK:
                return "El Roomba esta atascado, revisalo manualmente."

            if roombaPhase in (RoombaPhase.CHARGE, RoombaPhase.HM_USR_DOCK):
                return "El Roomba ya esta en casa."

            # La Roomba ignora el DOCK si viene sin pausa previa.
            await SendRoombaOrder(RoombaAction.PAUSE)
            await asyncio.sleep(_PAUSE_BEFORE_DOCK_SECONDS)
            await SendRoombaOrder(RoombaAction.DOCK)

            return "Enviando el Roomba a casa."
        except Exception as ex:
            return f"Error al enviar el Roomba a casa: {ex}"
# endregion

# region pause_roomba
async def PauseRoomba() -> str:
    async with _roombaLock:
        try:
            roombaPhase: RoombaPhase | None = await GetRoombaPhase()
            if roombaPhase is None:
                return "No se pudo obtener el estado del Roomba."

            if roombaPhase == RoombaPhase.STUCK:
                return "El Roomba esta atascado, revisalo manualmente."

            if roombaPhase != RoombaPhase.RUN:
                return "El Roomba no esta limpiando ahora mismo."

            await SendRoombaOrder(RoombaAction.PAUSE)

            return "Pausando el Roomba."
        except Exception as ex:
            return f"Error al pausar el Roomba: {ex}"
# endregion

# region reactive_roomba
async def ReactiveRoomba(roombaTarget: RoombaTarget) -> str:
    async with _roombaLock:
        try:
            roombaPhase: RoombaPhase | None = await GetRoombaPhase()
            if roombaPhase is None:
                return "No se pudo obtener el estado del Roomba."

            if roombaPhase == RoombaPhase.STUCK:
                return "El Roomba esta atascado, revisalo manualmente."

            if roombaPhase != RoombaPhase.STOP:
                return "El Roomba no esta pausado, no se puede reactivar."

            await SendRoombaOrder(RoombaAction.RESUME, roombaTarget)

            return "Reactivando el Roomba."
        except Exception as ex:
            return f"Error al reactivar el Roomba: {ex}"
# endregion

# region _build_payload
def _buildPayload(roombaAction: RoombaAction, roomIds: list[str], settings: Settings) -> RoombaPayload:
    command: str = roombaAction.name.lower()
    unixTime: int = int(datetime.now(timezone.utc).timestamp())

    if roombaAction != RoombaAction.START or not roomIds:
        return RoombaPayload(command = command, time = unixTime, initiator = "rmtApp")

    return RoombaPayload(
        command = command,
        time = unixTime,
        initiator = "rmtApp",
        ordered = 1,
        pmapId = settings.roombaPmapId,
        userPmapvId = settings.roombaPmapVersion,
        regions = [
            RoombaRegion(regionId = roomId, type = "rid", params = RoombaRegionParams())
            for roomId in roomIds
        ],
    )
# endregion

# region _get_roomba_rooms_ids
def _getRoombaRoomsIds(roombaTarget: RoombaTarget) -> list[str]:
    return _ROOM_IDS_BY_TARGET.get(roombaTarget, [])
# endregion

# region _parse_phase
def _parsePhase(phase: str | None) -> RoombaPhase | None:
    if phase is None:
        return None

    return _PHASES_BY_NAME.get(phase)
# endregion