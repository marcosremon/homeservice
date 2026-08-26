import asyncio
import json
import ssl
from dataclasses import dataclass, field
from datetime import datetime, time, timezone
from typing import Any

from aiomqtt import Client, MqttError, ProtocolVersion
from pydantic import TypeAdapter

from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateRequest import PatchRoombaStateRequest
from application.event.RoombaActivatedEvent import RoombaActivatedEvent
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

# Margen para que el shadow nos diga el pmap_id antes de mandar la orden.
_PMAP_DISCOVERY_SECONDS: float = 3.0

# Espera maxima a que el shadow conteste con la fase actual.
_PHASE_QUERY_TIMEOUT_SECONDS: float = 5.0

# Lo que escucha GetRoomInfo antes de rendirse.
_ROOM_INFO_LISTEN_SECONDS: float = 30.0

_payloadAdapter: TypeAdapter[RoombaPayload] = TypeAdapter(RoombaPayload)

@dataclass
class _RoombaState:
    """Lo ultimo que dijo el shadow de la Roomba."""
    pmapId: str = ""
    pmapVersion: str = ""
    battery: int = 0
    binFull: bool = False
    errorCode: int = 0
    errorMessage: str = ""
    phase: RoombaPhase = RoombaPhase.STOP
    phaseSeen: bool = False
    commandAccepted: bool = False
    activationEvents: list[PatchRoombaStateRequest] = field(default_factory = list)

# region _buildClient
def _buildClient(settings: Settings) -> Client:
    """Cliente MQTT contra la Roomba: TLS 1.2 sin validar el certificado.

    El certificado del aparato es autofirmado y su firmware solo habla TLS 1.2
    con cifrados que OpenSSL moderno considera debiles, de ahi el SECLEVEL=1.
    Es lo mismo que hace el WithCertificateValidationHandler(_ => true) de C#.
    """
    tlsContext: ssl.SSLContext = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
    tlsContext.check_hostname = False
    tlsContext.verify_mode = ssl.CERT_NONE
    tlsContext.minimum_version = ssl.TLSVersion.TLSv1_2
    tlsContext.maximum_version = ssl.TLSVersion.TLSv1_2

    try:
        tlsContext.set_ciphers("DEFAULT@SECLEVEL=1")
    except ssl.SSLError:
        pass

    return Client(
        hostname = settings.roombaId,
        port = int(settings.roombaPort or 8883),
        username = settings.roombaBlid,
        password = settings.roombaPasswd,
        identifier = settings.roombaBlid,
        protocol = ProtocolVersion.V31,
        tls_context = tlsContext,
    )
# endregion

# region _applyStateMessage
def _applyStateMessage(rawPayload: str, state: _RoombaState, roombaTarget: RoombaTarget | None) -> None:
    """Vuelca un mensaje del shadow sobre el estado que vamos acumulando.

    Es el ApplicationMessageReceivedAsync de C#. La Roomba manda el estado unas
    veces plano y otras dentro de state.reported, de ahi el desdoble.
    """
    try:
        root: dict[str, Any] = json.loads(rawPayload)
        stateNode: dict[str, Any] = root.get("state", {}).get("reported", root)

        if "pmapId" in stateNode:
            state.pmapId = stateNode["pmapId"] or state.pmapId

        if "userPmapvId" in stateNode:
            state.pmapVersion = stateNode["userPmapvId"] or state.pmapVersion

        if "batPct" in stateNode:
            state.battery = int(stateNode["batPct"])

        if isinstance(stateNode.get("bin"), dict) and "full" in stateNode["bin"]:
            state.binFull = bool(stateNode["bin"]["full"])

        missionNode: Any = stateNode.get("cleanMissionStatus")
        if not isinstance(missionNode, dict):
            return

        if "phase" in missionNode:
            phase: RoombaPhase | None = _parsePhase(missionNode["phase"])
            if phase is not None:
                state.phase = phase
                state.phaseSeen = True

            if missionNode["phase"] == "run" and not state.commandAccepted:
                state.commandAccepted = True
                state.activationEvents.append(BuildActivationRequest(
                    roombaPhase = state.phase,
                    roombaTarget = roombaTarget,
                    isActivation = True,
                    batteryPercent = state.battery,
                    binFull = state.binFull,
                    errorCode = state.errorCode,
                    errorMessage = state.errorMessage,
                    pmapId = state.pmapId,
                    userPmapvId = state.pmapVersion,
                ))

        if "error" in missionNode:
            state.errorCode = int(missionNode["error"])
            state.errorMessage = "" if state.errorCode == 0 else f"El Roomba reporto error: codigo {state.errorCode}"
    except Exception as ex:
        print(f"RoombaUtils -> error parseando mensaje MQTT: {ex}")
# endregion

# region _listenShadow
async def _listenShadow(client: Client, state: _RoombaState, roombaTarget: RoombaTarget | None) -> None:
    """Consume el shadow hasta que la cancelen."""
    async for message in client.messages:
        payload: Any = message.payload
        rawPayload: str = payload.decode("utf-8", errors = "replace") if isinstance(payload, bytes) else str(payload)

        _applyStateMessage(rawPayload, state, roombaTarget)
# endregion

# region get_room_info
async def GetRoomInfo() -> None:
    """Vuelca por consola todo lo que dice la Roomba durante 30 segundos.

    Solo sirve para averiguar los region_id a mano cuando cambias el mapa: mueve
    la Roomba desde la app de iRobot mientras esto escucha.
    """
    settings: Settings = GetSettings()
    if GeneralUtils.IsNullOrEmpty(settings.roombaId) or GeneralUtils.IsNullOrEmpty(settings.roombaBlid):
        print("RoombaUtils -> GetRoomInfo -> Configuracion incompleta (ip o blid vacios)")
        return

    try:
        async with _buildClient(settings) as client:
            await client.subscribe(f"$aws/things/{settings.roombaBlid}/shadow/update")
            await client.subscribe("wifistat")
            await client.subscribe("#")

            print(f"Escuchando {_ROOM_INFO_LISTEN_SECONDS:.0f} segundos... mueve el Roomba desde la app para forzar mensajes")

            async with asyncio.timeout(_ROOM_INFO_LISTEN_SECONDS):
                async for message in client.messages:
                    payload: Any = message.payload
                    rawPayload: str = payload.decode("utf-8", errors = "replace") if isinstance(payload, bytes) else str(payload)

                    print(f"\n=== MENSAJE RAW ===\n{rawPayload}\n==================")

                    if "regions" in rawPayload or "pmapId" in rawPayload or "pmap" in rawPayload:
                        print("^^^ CONTIENE INFO DE MAPA ^^^")
    except TimeoutError:
        print("RoombaUtils -> GetRoomInfo -> fin de la escucha")
    except MqttError as ex:
        print(f"RoombaUtils -> GetRoomInfo -> {ex}")
# endregion

# region get_roomba_phase
async def GetRoombaPhase() -> RoombaPhase | None:
    """Fase actual de la Roomba, o None si no se puede consultar.

    None significa "no se pudo preguntar" (aparato apagado, red caida, timeout);
    todas las ordenes de abajo lo tratan como motivo para abortar.
    """
    settings: Settings = GetSettings()
    if GeneralUtils.IsNullOrEmpty(settings.roombaId) or GeneralUtils.IsNullOrEmpty(settings.roombaBlid):
        print("RoombaUtils -> GetRoombaPhase -> Configuracion incompleta (ip o blid vacios)")
        return None

    state: _RoombaState = _RoombaState()
    try:
        async with _buildClient(settings) as client:
            await client.subscribe(f"$aws/things/{settings.roombaBlid}/shadow/update")

            async with asyncio.timeout(_PHASE_QUERY_TIMEOUT_SECONDS):
                async for message in client.messages:
                    payload: Any = message.payload
                    rawPayload: str = payload.decode("utf-8", errors = "replace") if isinstance(payload, bytes) else str(payload)

                    _applyStateMessage(rawPayload, state, None)

                    if state.phaseSeen:
                        return state.phase
    except TimeoutError:
        print("RoombaUtils -> GetRoombaPhase -> la Roomba no contesto a tiempo")
    except MqttError as ex:
        print(f"RoombaUtils -> GetRoombaPhase -> {ex}")

    return state.phase if state.phaseSeen else None
# endregion

# region send_roomba_order
async def SendRoombaOrder(roombaAction: RoombaAction, roombaTarget: RoombaTarget | None = None) -> None:
    """Publica la orden en cmd/{blid}/delta y espera la confirmacion del shadow.

    En C# son dos sobrecargas (con y sin RoombaTarget); en Python el target es
    opcional, que es lo mismo con una sola funcion.
    """
    settings: Settings = GetSettings()

    if (GeneralUtils.IsNullOrEmpty(settings.roombaId) or
        GeneralUtils.IsNullOrEmpty(settings.roombaBlid) or
        GeneralUtils.IsNullOrEmpty(settings.roombaPasswd)
    ):
        print("RoombaUtils -> SendRoombaOrder -> Configuracion incompleta (ip, blid o pass vacios)")
        return

    roomIds: list[str] = _getRoombaRoomsIds(roombaTarget) if roombaTarget is not None else []
    state: _RoombaState = _RoombaState()

    try:
        async with _buildClient(settings) as client:
            await client.subscribe(f"$aws/things/{settings.roombaBlid}/shadow/update")
            await client.subscribe("wifistat")

            listener: asyncio.Task[None] = asyncio.create_task(_listenShadow(client, state, roombaTarget))
            try:
                # La Roomba anuncia su mapa nada mas conectar; hay que darle un
                # momento antes de mandar una orden por regiones.
                await asyncio.sleep(_PMAP_DISCOVERY_SECONDS)

                finalPmapId: str = state.pmapId or settings.roombaPmapId
                finalPmapVersion: str = state.pmapVersion or settings.roombaPmapVersion

                if GeneralUtils.IsNullOrEmpty(finalPmapId):
                    print("RoombaUtils -> SendRoombaOrder -> No se pudo obtener pmap_id. Abortando.")
                    return

                print(f"  -> Usando pmap_id: {finalPmapId} / version: {finalPmapVersion}")
                print(f"  -> Regiones: [{', '.join(roomIds)}]")

                payload: RoombaPayload = _buildPayload(roombaAction, roomIds, finalPmapId, finalPmapVersion)

                await client.publish(f"cmd/{settings.roombaBlid}/delta", _payloadAdapter.dump_json(payload, by_alias = True, exclude_none = True))
                print("  -> Orden enviada, esperando confirmacion...")

                elapsed: float = 0.0
                while not state.commandAccepted and elapsed < _COMMAND_CONFIRMATION_TIMEOUT_SECONDS:
                    await asyncio.sleep(0.5)
                    elapsed += 0.5

                # El evento de activacion lo arma el listener en cuanto ve phase
                # "run"; aqui solo se publica, para que no salga desde una tarea
                # que puede morir con la conexion.
                for activationEvent in state.activationEvents:
                    RoombaActivatedEvent.Publish(activationEvent)

                RoombaActivatedEvent.Publish(BuildActivationRequest(
                    roombaPhase = state.phase,
                    roombaTarget = roombaTarget,
                    batteryPercent = state.battery,
                    binFull = state.binFull,
                    errorCode = state.errorCode,
                    errorMessage = state.errorMessage,
                    pmapId = finalPmapId,
                    userPmapvId = finalPmapVersion,
                ))

                if state.commandAccepted:
                    print("  -> Roomba confirmo inicio de limpieza (phase: run)")
                else:
                    print("  -> La Roomba no confirmo la orden dentro del margen")
            finally:
                listener.cancel()
    except MqttError as ex:
        print(f"RoombaUtils -> SendRoombaOrder -> {ex}")
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

            if roombaPhase == RoombaPhase.RUN or roombaPhase == RoombaPhase.HM_USR_DOCK:
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

            if roombaPhase == RoombaPhase.RUN or roombaPhase == RoombaPhase.HM_USR_DOCK:
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

            if roombaPhase == RoombaPhase.CHARGE or roombaPhase == RoombaPhase.HM_USR_DOCK:
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
def _buildPayload(roombaAction: RoombaAction, roomIds: list[str], pmapId: str, pmapVersion: str) -> RoombaPayload:
    command: str = roombaAction.name.lower()
    unixTime: int = int(datetime.now(timezone.utc).timestamp())

    if roombaAction != RoombaAction.START or not roomIds:
        return RoombaPayload(command = command, time = unixTime, initiator = "rmtApp")

    return RoombaPayload(
        command = command,
        time = unixTime,
        initiator = "rmtApp",
        ordered = 1,
        pmapId = pmapId,
        userPmapvId = pmapVersion,
        regions = [
            RoombaRegion(regionId = roomId, type = "rid", params = RoombaRegionParams())
            for roomId in roomIds
        ],
    )
# endregion

# region _get_roomba_rooms_ids
def _getRoombaRoomsIds(roombaTarget: RoombaTarget) -> list[str]:
    match roombaTarget:
        case RoombaTarget.KITCHEN:
            return ["11"]

        case RoombaTarget.DIEGO:
            return ["16"]

        case RoombaTarget.MARCOS:
            return ["21"]

        case RoombaTarget.KITCHEN_AND_GRANDMOTHER:
            return ["4", "11"]

        case RoombaTarget.BEDROOMS:
            return ["21", "16"]

        case RoombaTarget.BEDROOM_AND_TOILET:
            return ["23", "25"]

        case RoombaTarget.HALLWAY_AND_TOILET:
            return ["19", "22", "24"]

        case RoombaTarget.LIVING_ROOM:
            return ["1", "18"]

        case _:
            return []
# endregion

# region _parse_phase
def _parsePhase(phase: str | None) -> RoombaPhase | None:
    match phase:
        case "charge":
            return RoombaPhase.CHARGE

        case "run":
            return RoombaPhase.RUN

        case "stop":
            return RoombaPhase.STOP

        case "hmUsrDock":
            return RoombaPhase.HM_USR_DOCK

        case "stuck":
            return RoombaPhase.STUCK

        case _:
            return None
# endregion