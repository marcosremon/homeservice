import asyncio
from datetime import datetime, time, timezone

from application.data_transfer_object.roomba.patch_roomba_state.patch_roomba_state_request import PatchRoombaStateRequest
from domain.model.enum.Roomba.roomba_action import RoombaAction
from domain.model.enum.Roomba.roomba_phase import RoombaPhase
from domain.model.enum.Roomba.roomba_target import RoombaTarget
from infraestructure.gateway.roomba.payload.roomba_payload import RoombaPayload
from infraestructure.gateway.roomba.payload.roomba_region import RoombaRegion
from infraestructure.gateway.roomba.payload.roomba_region_params import RoombaRegionParams
from transversal.common.configuration.settings import Settings, get_settings
from transversal.common.utils.general_utils import GeneralUtils

# Equivalente a SemaphoreSlim(1, 1): la Roomba no admite dos ordenes a la vez.
_roomba_lock: asyncio.Lock = asyncio.Lock()

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
async def get_room_info() -> None:
    """Vuelca por consola el mapa de habitaciones que conoce la Roomba.

    Solo sirve para averiguar los region_id a mano cuando cambias el mapa.
    """
    # TODO (MQTT): suscribirse a $aws/things/{blid}/shadow/update y volcar el
    # nodo pmaps del mensaje recibido.
    print("RoombaUtils -> get_room_info -> pendiente del transporte MQTT.")
# endregion

# region get_roomba_phase
async def get_roomba_phase() -> RoombaPhase | None:
    """Fase actual de la Roomba, o None si no se puede consultar."""
    # TODO (MQTT): conectar, escuchar cleanMissionStatus.phase y devolver
    # _parse_phase(phase). None significa "no se pudo consultar", y todas las
    # funciones de abajo ya lo tratan como tal.
    return None
# endregion

# region send_roomba_order
async def send_roomba_order(roomba_action: RoombaAction, roomba_target: RoombaTarget | None = None) -> None:
    """Publica la orden en cmd/{blid}/delta.

    En C# son dos sobrecargas (con y sin RoombaTarget); en Python el target es
    opcional, que es lo mismo con una sola funcion.
    """
    settings: Settings = get_settings()

    if (GeneralUtils.is_null_or_empty(settings.roomba_id) or
        GeneralUtils.is_null_or_empty(settings.roomba_blid) or
        GeneralUtils.is_null_or_empty(settings.roomba_passwd)
    ):
        print("RoombaUtils -> send_roomba_order -> Configuracion incompleta (ip, blid o pass vacios)")
        return

    room_ids: list[str] = _get_roomba_rooms_ids(roomba_target) if roomba_target is not None else []
    payload: RoombaPayload = _build_payload(roomba_action, room_ids, settings)

    # TODO (MQTT): conectar con tls a {roomba_id}:{roomba_port} con credenciales
    # (blid, passwd), publicar `payload` en cmd/{blid}/delta y escuchar el shadow
    # hasta que phase == "run" o pasen _COMMAND_CONFIRMATION_TIMEOUT_SECONDS.
    # Cuando llegue esa confirmacion hay que publicar el evento de activacion:
    #     RoombaActivatedEvent.publish(build_activation_request(...))
    # y al terminar la espera, el evento de estado final.
    print(f"RoombaUtils -> send_roomba_order -> pendiente del transporte MQTT. Payload: {payload}")
# endregion

# region build_activation_request
def build_activation_request(
    roomba_phase: RoombaPhase,
    roomba_target: RoombaTarget | None = None,
    is_activation: bool = False,
    battery_percent: int = 0,
    bin_full: bool = False,
    error_code: int = 0,
    error_message: str = "",
    pmap_id: str = "",
    user_pmapv_id: str = "",
) -> PatchRoombaStateRequest:
    return PatchRoombaStateRequest(
        event_time = datetime.now(timezone.utc),
        is_activation = is_activation,
        target = roomba_target if roomba_target is not None else RoombaTarget.FULL_HOUSE,
        phase = roomba_phase,
        battery_percent = battery_percent,
        bin_full = bin_full,
        error_code = error_code,
        error_message = error_message,
        pmap_id = pmap_id,
        user_pmapv_id = user_pmapv_id,
        is_online = True,
    )
# endregion

# region start_roomba_if_house_is_empty
async def start_roomba_if_house_is_empty(last_roomba_activation: datetime) -> None:
    async with _roomba_lock:
        try:
            now: datetime = datetime.now()

            is_valid_time: bool = _AUTO_START_FROM <= now.time() <= _AUTO_START_TO
            is_activated_today: bool = last_roomba_activation.date() == now.date()

            if is_activated_today:
                print("Roomba ya se activo hoy, omitiendo.")
                return

            if not is_valid_time:
                print(f"Fuera de horario ({_AUTO_START_FROM:%H:%M} - {_AUTO_START_TO:%H:%M}), omitiendo.")
                return

            roomba_phase: RoombaPhase | None = await get_roomba_phase()
            if roomba_phase is None:
                print("No se pudo obtener el estado del Roomba, abortando.")
                return

            if roomba_phase == RoombaPhase.STUCK:
                print("Roomba atascado, revisalo manualmente.")
                return

            if roomba_phase in (RoombaPhase.RUN, RoombaPhase.HM_USR_DOCK):
                print(f"start_roomba_if_house_is_empty ignorado - fase actual: {roomba_phase.name}")
                return

            await send_roomba_order(RoombaAction.START, RoombaTarget.FULL_HOUSE)
        except Exception as ex:
            print(f"Error en RoombaUtils -> start_roomba_if_house_is_empty: {ex}")
# endregion

# region start_roomba
async def start_roomba(roomba_target: RoombaTarget) -> str:
    async with _roomba_lock:
        try:
            roomba_phase: RoombaPhase | None = await get_roomba_phase()
            if roomba_phase is None:
                return "No se pudo obtener el estado del Roomba."

            if roomba_phase == RoombaPhase.STUCK:
                return "El Roomba esta atascado, revisalo manualmente."

            if roomba_phase in (RoombaPhase.RUN, RoombaPhase.HM_USR_DOCK):
                return "El Roomba ya esta limpiando."

            await send_roomba_order(RoombaAction.START, roomba_target)

            if roomba_target == RoombaTarget.FULL_HOUSE:
                return "Iniciando limpieza de la casa completa."

            return f"Iniciando limpieza de {roomba_target.name.lower().replace('_', ' ')}."
        except Exception as ex:
            return f"Error al iniciar el Roomba: {ex}"
# endregion

# region send_roomba_home
async def send_roomba_home() -> str:
    async with _roomba_lock:
        try:
            roomba_phase: RoombaPhase | None = await get_roomba_phase()
            if roomba_phase is None:
                return "No se pudo obtener el estado del Roomba."

            if roomba_phase == RoombaPhase.STUCK:
                return "El Roomba esta atascado, revisalo manualmente."

            if roomba_phase in (RoombaPhase.CHARGE, RoombaPhase.HM_USR_DOCK):
                return "El Roomba ya esta en casa."

            # La Roomba ignora el DOCK si viene sin pausa previa.
            await send_roomba_order(RoombaAction.PAUSE)
            await asyncio.sleep(_PAUSE_BEFORE_DOCK_SECONDS)
            await send_roomba_order(RoombaAction.DOCK)

            return "Enviando el Roomba a casa."
        except Exception as ex:
            return f"Error al enviar el Roomba a casa: {ex}"
# endregion

# region pause_roomba
async def pause_roomba() -> str:
    async with _roomba_lock:
        try:
            roomba_phase: RoombaPhase | None = await get_roomba_phase()
            if roomba_phase is None:
                return "No se pudo obtener el estado del Roomba."

            if roomba_phase == RoombaPhase.STUCK:
                return "El Roomba esta atascado, revisalo manualmente."

            if roomba_phase != RoombaPhase.RUN:
                return "El Roomba no esta limpiando ahora mismo."

            await send_roomba_order(RoombaAction.PAUSE)

            return "Pausando el Roomba."
        except Exception as ex:
            return f"Error al pausar el Roomba: {ex}"
# endregion

# region reactive_roomba
async def reactive_roomba(roomba_target: RoombaTarget) -> str:
    async with _roomba_lock:
        try:
            roomba_phase: RoombaPhase | None = await get_roomba_phase()
            if roomba_phase is None:
                return "No se pudo obtener el estado del Roomba."

            if roomba_phase == RoombaPhase.STUCK:
                return "El Roomba esta atascado, revisalo manualmente."

            if roomba_phase != RoombaPhase.STOP:
                return "El Roomba no esta pausado, no se puede reactivar."

            await send_roomba_order(RoombaAction.RESUME, roomba_target)

            return "Reactivando el Roomba."
        except Exception as ex:
            return f"Error al reactivar el Roomba: {ex}"
# endregion

# region _build_payload
def _build_payload(roomba_action: RoombaAction, room_ids: list[str], settings: Settings) -> RoombaPayload:
    command: str = roomba_action.name.lower()
    unix_time: int = int(datetime.now(timezone.utc).timestamp())

    if roomba_action != RoombaAction.START or not room_ids:
        return RoombaPayload(command = command, time = unix_time, initiator = "rmtApp")

    return RoombaPayload(
        command = command,
        time = unix_time,
        initiator = "rmtApp",
        ordered = 1,
        pmap_id = settings.roomba_pmap_id,
        user_pmapv_id = settings.roomba_pmap_version,
        regions = [
            RoombaRegion(region_id = room_id, type = "rid", params = RoombaRegionParams())
            for room_id in room_ids
        ],
    )
# endregion

# region _get_roomba_rooms_ids
def _get_roomba_rooms_ids(roomba_target: RoombaTarget) -> list[str]:
    return _ROOM_IDS_BY_TARGET.get(roomba_target, [])
# endregion

# region _parse_phase
def _parse_phase(phase: str | None) -> RoombaPhase | None:
    if phase is None:
        return None

    return _PHASES_BY_NAME.get(phase)
# endregion