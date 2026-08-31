import asyncio
from contextlib import AsyncExitStack
import json
import ssl
from datetime import datetime, time, timezone
from typing import Any
from aiomqtt import Client, MqttError, ProtocolVersion
from pydantic import TypeAdapter
from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateRequest import PatchRoombaStateRequest
from application.event.RoombaActivatedEvent import RoombaActivatedEvent
from domain.model.enum.roomba.RoombaAction import RoombaAction
from domain.model.enum.roomba.RoombaPhase import RoombaPhase
from domain.model.enum.roomba.RoombaTarget import RoombaTarget
from infraestructure.gateway.roomba.payload.RoombaPayload import RoombaPayload
from infraestructure.gateway.roomba.payload.RoombaRegion import RoombaRegion
from infraestructure.gateway.roomba.payload.RoombaRegionParams import RoombaRegionParams
from transversal.common.configuration.Settings import Settings, GetSettings
from transversal.common.roomba.RoombaState import RoombaState
from transversal.common.utils.GeneralUtils import GeneralUtils

class RoombaUtils:

    _roombaLock: asyncio.Lock = asyncio.Lock()

    # Ventana en la que se permite el arranque automatico.
    _AUTO_START_FROM: time = time(8, 0)
    _AUTO_START_TO: time = time(21, 0)

    # Espera maxima a que la roomba confirme la orden (phase: run).
    _COMMAND_CONFIRMATION_TIMEOUT_SECONDS: float = 8.0

    # Pausa entre PAUSE y DOCK al mandarla a casa.
    _PAUSE_BEFORE_DOCK_SECONDS: float = 3.0

    # Margen para que el shadow nos diga el pmap_id antes de mandar la orden.
    _PMAP_DISCOVERY_SECONDS: float = 3.0

    # La roomba solo acepta un cliente MQTT y tarda ~2,5s en volver a escuchar
    # tras cerrar una conexion: sin reintentos, la segunda orden seguida se come
    # un ECONNREFUSED.
    _CONNECT_RETRY_ATTEMPTS: int = 5
    _CONNECT_RETRY_SECONDS: float = 2.5

    # Espera maxima a que el shadow conteste con la fase actual.
    _PHASE_QUERY_TIMEOUT_SECONDS: float = 5.0

    # Lo que escucha GetRoomInfo antes de rendirse.
    _ROOM_INFO_LISTEN_SECONDS: float = 30.0

    _payloadAdapter: TypeAdapter[RoombaPayload] = TypeAdapter(RoombaPayload)

    # region _buildClient
    @staticmethod
    def _buildClient(settings: Settings) -> Client:
        """Cliente MQTT contra la roomba: TLS 1.2 sin validar el certificado.

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
            protocol = ProtocolVersion.V311,
            tls_context = tlsContext,
        )
    # endregion

    # region _connect
    @classmethod
    async def _connect(cls, settings: Settings, exitStack: AsyncExitStack) -> Client:
        """Conecta reintentando: tras cerrar una sesion la roomba rechaza un rato."""
        lastError: Exception = MqttError("sin intentos")

        for attempt in range(cls._CONNECT_RETRY_ATTEMPTS):
            try:
                return await exitStack.enter_async_context(cls._buildClient(settings))
            except (MqttError, OSError) as ex:
                lastError = ex
                if attempt < cls._CONNECT_RETRY_ATTEMPTS - 1:
                    await asyncio.sleep(cls._CONNECT_RETRY_SECONDS)

        raise MqttError(f"no se pudo conectar tras {cls._CONNECT_RETRY_ATTEMPTS} intentos: {lastError}")
    # endregion

    # region _decodePayload
    @staticmethod
    def _decodePayload(payload: Any) -> str:
        return payload.decode("utf-8", errors = "replace") if isinstance(payload, bytes) else str(payload)
    # endregion

    # region _applyStateMessage
    @classmethod
    def _applyStateMessage(cls, rawPayload: str, roombaState: RoombaState, roombaTarget: RoombaTarget | None, roombaAction: RoombaAction | None = None) -> None:
        """Vuelca un mensaje del shadow sobre el estado que vamos acumulando.

        Es el ApplicationMessageReceivedAsync de C#. La roomba manda el estado unas
        veces plano y otras dentro de state.reported, de ahi el desdoble.
        """
        try:
            root: dict[str, Any] = json.loads(rawPayload)
            stateNode: dict[str, Any] = root.get("state", {}).get("reported", root)

            if "pmapId" in stateNode:
                roombaState.pmapId = stateNode["pmapId"] or roombaState.pmapId

            if "userPmapvId" in stateNode:
                roombaState.pmapVersion = stateNode["userPmapvId"] or roombaState.pmapVersion

            if "batPct" in stateNode:
                roombaState.battery = int(stateNode["batPct"])

            if isinstance(stateNode.get("bin"), dict) and "full" in stateNode["bin"]:
                roombaState.binFull = bool(stateNode["bin"]["full"])

            missionNode: Any = stateNode.get("cleanMissionStatus")
            if not isinstance(missionNode, dict):
                return

            # El error se vuelca antes que la fase: un mismo mensaje puede traer
            # "phase": "run" y un "error", y el evento de activacion tiene que
            # salir con el error de este mensaje, no con el del anterior.
            if "error" in missionNode:
                roombaState.errorCode = int(missionNode["error"])
                roombaState.errorMessage = "" if roombaState.errorCode == 0 else f"El roomba reporto error: codigo {roombaState.errorCode}"

            if "phase" in missionNode:
                phase: RoombaPhase | None = cls._parsePhase(missionNode["phase"])
                if phase is not None:
                    roombaState.phase = phase
                    roombaState.phaseSeen = True

                # Solo un START cuenta como activacion: al pausar o mandarla a
                # casa el shadow sigue diciendo "run" un rato, y eso machacaba
                # last_roomba_activation con la hora de la pausa.
                if phase == RoombaPhase.RUN and not roombaState.commandAccepted and roombaAction == RoombaAction.START:
                    roombaState.commandAccepted = True
                    roombaState.activationEvents.append(cls.BuildActivationRequest(
                        roombaPhase = RoombaPhase.RUN,
                        roombaTarget = roombaTarget,
                        isActivation = True,
                        batteryPercent = roombaState.battery,
                        binFull = roombaState.binFull,
                        errorCode = roombaState.errorCode,
                        errorMessage = roombaState.errorMessage,
                        pmapId = roombaState.pmapId,
                        userPmapvId = roombaState.pmapVersion,
                    ))

                # Vuelta a la base o a cargar: la mision ha terminado.
                if phase in (RoombaPhase.CHARGE, RoombaPhase.HM_USR_DOCK) and not roombaState.finishSeen:
                    roombaState.finishSeen = True
                    roombaState.activationEvents.append(cls.BuildActivationRequest(
                        roombaPhase = phase,
                        roombaTarget = roombaTarget,
                        isFinished = True,
                        batteryPercent = roombaState.battery,
                        binFull = roombaState.binFull,
                        errorCode = roombaState.errorCode,
                        errorMessage = roombaState.errorMessage,
                        pmapId = roombaState.pmapId,
                        userPmapvId = roombaState.pmapVersion,
                    ))
        except Exception as ex:
            print(f"RoombaUtils -> error parseando mensaje MQTT: {ex}")
    # endregion

    # region _listenShadow
    @classmethod
    async def _listenShadow(cls, client: Client, roombaState: RoombaState, roombaTarget: RoombaTarget | None, roombaAction: RoombaAction | None = None) -> None:
        async for message in client.messages:
            cls._applyStateMessage(cls._decodePayload(message.payload), roombaState, roombaTarget, roombaAction)
    # endregion

    # region GetRoomInfo
    @classmethod
    async def GetRoomInfo(cls) -> None:
        """Vuelca por consola todo lo que dice la roomba durante 30 segundos.

        Solo sirve para averiguar los region_id a mano cuando cambias el mapa: mueve
        la roomba desde la app de iRobot mientras esto escucha.
        """
        settings: Settings = GetSettings()
        if GeneralUtils.IsNullOrEmpty(settings.roombaId) or GeneralUtils.IsNullOrEmpty(settings.roombaBlid):
            print("RoombaUtils -> GetRoomInfo -> Configuracion incompleta (ip o blid vacios)")
            return

        try:
            async with AsyncExitStack() as exitStack:
                client: Client = await cls._connect(settings, exitStack)

                await client.subscribe(f"$aws/things/{settings.roombaBlid}/shadow/update")
                await client.subscribe("wifistat")
                await client.subscribe("#")

                print(f"Escuchando {cls._ROOM_INFO_LISTEN_SECONDS:.0f} segundos... mueve el roomba desde la app para forzar mensajes")

                async with asyncio.timeout(cls._ROOM_INFO_LISTEN_SECONDS):
                    async for message in client.messages:
                        rawPayload: str = cls._decodePayload(message.payload)

                        print(f"\n=== MENSAJE RAW ===\n{rawPayload}\n==================")

                        if "regions" in rawPayload or "pmapId" in rawPayload or "pmap" in rawPayload:
                            print("^^^ CONTIENE INFO DE MAPA ^^^")
        except TimeoutError:
            print("RoombaUtils -> GetRoomInfo -> fin de la escucha")
        except MqttError as ex:
            print(f"RoombaUtils -> GetRoomInfo -> {ex}")
    # endregion

    # region GetRoombaPhase
    @classmethod
    async def GetRoombaPhase(cls) -> RoombaPhase | None:
        """Fase actual de la roomba, o None si no se puede consultar.

        None significa "no se pudo preguntar" (aparato apagado, red caida, timeout);
        todas las ordenes de abajo lo tratan como motivo para abortar.
        """
        settings: Settings = GetSettings()
        if GeneralUtils.IsNullOrEmpty(settings.roombaId) or GeneralUtils.IsNullOrEmpty(settings.roombaBlid):
            print("RoombaUtils -> GetRoombaPhase -> Configuracion incompleta (ip o blid vacios)")
            return None

        roombaState: RoombaState = RoombaState()
        try:
            async with AsyncExitStack() as exitStack:
                client: Client = await cls._connect(settings, exitStack)

                await client.subscribe(f"$aws/things/{settings.roombaBlid}/shadow/update")

                async with asyncio.timeout(cls._PHASE_QUERY_TIMEOUT_SECONDS):
                    async for message in client.messages:
                        cls._applyStateMessage(cls._decodePayload(message.payload), roombaState, None)

                        if roombaState.phaseSeen:
                            return roombaState.phase
        except TimeoutError:
            print("RoombaUtils -> GetRoombaPhase -> la roomba no contesto a tiempo")
        except MqttError as ex:
            print(f"RoombaUtils -> GetRoombaPhase -> {ex}")

        return roombaState.phase if roombaState.phaseSeen else None
    # endregion

    # region SendRoombaOrder
    @classmethod
    async def SendRoombaOrder(cls, roombaAction: RoombaAction, roombaTarget: RoombaTarget | None = None) -> None:
        settings: Settings = GetSettings()

        if (GeneralUtils.IsNullOrEmpty(settings.roombaId) or
            GeneralUtils.IsNullOrEmpty(settings.roombaBlid) or
            GeneralUtils.IsNullOrEmpty(settings.roombaPasswd)
        ):
            print("RoombaUtils -> SendRoombaOrder -> Configuracion incompleta (ip, blid o pass vacios)")
            return

        roomIds: list[str] = cls._getRoombaRoomsIds(roombaTarget) if roombaTarget is not None else []
        roombaState: RoombaState = RoombaState()

        try:
            async with AsyncExitStack() as exitStack:
                client: Client = await cls._connect(settings, exitStack)

                await client.subscribe(f"$aws/things/{settings.roombaBlid}/shadow/update")
                await client.subscribe("wifistat")

                listener: asyncio.Task[None] = asyncio.create_task(cls._listenShadow(client, roombaState, roombaTarget, roombaAction))
                try:
                    # La roomba anuncia su mapa nada mas conectar; hay que darle un
                    # momento antes de mandar una orden por regiones.
                    await asyncio.sleep(cls._PMAP_DISCOVERY_SECONDS)

                    finalPmapId: str = roombaState.pmapId or settings.roombaPmapId
                    finalPmapVersion: str = roombaState.pmapVersion or settings.roombaPmapVersion

                    if GeneralUtils.IsNullOrEmpty(finalPmapId):
                        print("RoombaUtils -> SendRoombaOrder -> No se pudo obtener pmap_id. Abortando.")
                        return

                    print(f"  -> Usando pmap_id: {finalPmapId} / version: {finalPmapVersion}")
                    print(f"  -> Regiones: [{', '.join(roomIds)}]")

                    payload: RoombaPayload = cls._buildPayload(roombaAction, roomIds, finalPmapId, finalPmapVersion)

                    await client.publish(f"cmd/{settings.roombaBlid}/delta", cls._payloadAdapter.dump_json(payload, by_alias = True, exclude_none = True))
                    print("  -> Orden enviada, esperando confirmacion...")

                    elapsed: float = 0.0
                    while not roombaState.commandAccepted and elapsed < cls._COMMAND_CONFIRMATION_TIMEOUT_SECONDS:
                        await asyncio.sleep(0.5)
                        elapsed += 0.5

                    # El evento de activacion lo arma el listener en cuanto ve phase
                    # "run"; aqui solo se publica, para que no salga desde una tarea
                    # que puede morir con la conexion.
                    for activationEvent in roombaState.activationEvents:
                        RoombaActivatedEvent.Publish(activationEvent)

                    RoombaActivatedEvent.Publish(cls.BuildActivationRequest(
                        roombaPhase = roombaState.phase,
                        roombaTarget = roombaTarget,
                        isFinished = roombaState.phase in (RoombaPhase.CHARGE, RoombaPhase.HM_USR_DOCK) and not roombaState.finishSeen,
                        batteryPercent = roombaState.battery,
                        binFull = roombaState.binFull,
                        errorCode = roombaState.errorCode,
                        errorMessage = roombaState.errorMessage,
                        pmapId = finalPmapId,
                        userPmapvId = finalPmapVersion,
                    ))

                    if roombaState.commandAccepted:
                        print("  -> roomba confirmo inicio de limpieza (phase: run)")
                    else:
                        print("  -> La roomba no confirmo la orden dentro del margen")
                finally:
                    listener.cancel()
        except MqttError as ex:
            print(f"RoombaUtils -> SendRoombaOrder -> {ex}")
    # endregion

    # region BuildActivationRequest
    @staticmethod
    def BuildActivationRequest(
        roombaPhase: RoombaPhase,
        roombaTarget: RoombaTarget | None = None,
        isActivation: bool = False,
        isFinished: bool = False,
        batteryPercent: int = 0,
        binFull: bool = False,
        errorCode: int = 0,
        errorMessage: str = "",
        pmapId: str = "",
        userPmapvId: str = "",
    ) -> PatchRoombaStateRequest:
        return PatchRoombaStateRequest(
            isActivation = isActivation,
            isFinished = isFinished,
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

    # region StartRoombaIfHouseIsEmpty
    @classmethod
    async def StartRoombaIfHouseIsEmpty(cls, lastRoombaActivation: datetime) -> None:
        async with cls._roombaLock:
            try:
                now: datetime = datetime.now()

                isValidTime: bool = cls._AUTO_START_FROM <= now.time() <= cls._AUTO_START_TO
                isActivatedToday: bool = lastRoombaActivation.date() == now.date()

                if isActivatedToday:
                    print("roomba ya se activo hoy, omitiendo.")
                    return

                if not isValidTime:
                    print(f"Fuera de horario ({cls._AUTO_START_FROM:%H:%M} - {cls._AUTO_START_TO:%H:%M}), omitiendo.")
                    return

                roombaPhase: RoombaPhase | None = await cls.GetRoombaPhase()
                if roombaPhase is None:
                    print("No se pudo obtener el estado del roomba, abortando.")
                    return

                if roombaPhase == RoombaPhase.STUCK:
                    print("roomba atascado, revisalo manualmente.")
                    return

                if roombaPhase == RoombaPhase.RUN or roombaPhase == RoombaPhase.HM_USR_DOCK:
                    print(f"StartRoombaIfHouseIsEmpty ignorado - fase actual: {roombaPhase.name}")
                    return

                await cls.SendRoombaOrder(RoombaAction.START, RoombaTarget.FULL_HOUSE)
            except Exception as ex:
                print(f"Error en RoombaUtils -> StartRoombaIfHouseIsEmpty: {ex}")
    # endregion

    # region StartRoomba
    @classmethod
    async def StartRoomba(cls, roombaTarget: RoombaTarget) -> str:
        async with cls._roombaLock:
            try:
                roombaPhase: RoombaPhase | None = await cls.GetRoombaPhase()
                if roombaPhase is None:
                    return "No se pudo obtener el estado del roomba."

                if roombaPhase == RoombaPhase.STUCK:
                    return "El roomba esta atascado, revisalo manualmente."

                if roombaPhase == RoombaPhase.RUN or roombaPhase == RoombaPhase.HM_USR_DOCK:
                    return "El roomba ya esta limpiando."

                await cls.SendRoombaOrder(RoombaAction.START, roombaTarget)

                if roombaTarget == RoombaTarget.FULL_HOUSE:
                    return "Iniciando limpieza de la casa completa."

                return f"Iniciando limpieza de {roombaTarget.name.lower().replace('_', ' ')}."
            except Exception as ex:
                return f"Error al iniciar el roomba: {ex}"
    # endregion

    # region SendRoombaHome
    @classmethod
    async def SendRoombaHome(cls) -> str:
        async with cls._roombaLock:
            try:
                roombaPhase: RoombaPhase | None = await cls.GetRoombaPhase()
                if roombaPhase is None:
                    return "No se pudo obtener el estado del roomba."

                if roombaPhase == RoombaPhase.STUCK:
                    return "El roomba esta atascado, revisalo manualmente."

                if roombaPhase == RoombaPhase.CHARGE or roombaPhase == RoombaPhase.HM_USR_DOCK:
                    return "El roomba ya esta en casa."

                # La roomba ignora el DOCK si viene sin pausa previa.
                await cls.SendRoombaOrder(RoombaAction.PAUSE)
                await asyncio.sleep(cls._PAUSE_BEFORE_DOCK_SECONDS)
                await cls.SendRoombaOrder(RoombaAction.DOCK)

                return "Enviando el roomba a casa."
            except Exception as ex:
                return f"Error al enviar el roomba a casa: {ex}"
    # endregion

    # region PauseRoomba
    @classmethod
    async def PauseRoomba(cls) -> str:
        async with cls._roombaLock:
            try:
                roombaPhase: RoombaPhase | None = await cls.GetRoombaPhase()
                if roombaPhase is None:
                    return "No se pudo obtener el estado del roomba."

                if roombaPhase == RoombaPhase.STUCK:
                    return "El roomba esta atascado, revisalo manualmente."

                if roombaPhase != RoombaPhase.RUN:
                    return "El roomba no esta limpiando ahora mismo."

                await cls.SendRoombaOrder(RoombaAction.PAUSE)

                return "Pausando el roomba."
            except Exception as ex:
                return f"Error al pausar el roomba: {ex}"
    # endregion

    # region ReactiveRoomba
    @classmethod
    async def ReactiveRoomba(cls, roombaTarget: RoombaTarget) -> str:
        async with cls._roombaLock:
            try:
                roombaPhase: RoombaPhase | None = await cls.GetRoombaPhase()
                if roombaPhase is None:
                    return "No se pudo obtener el estado del roomba."

                if roombaPhase == RoombaPhase.STUCK:
                    return "El roomba esta atascado, revisalo manualmente."

                if roombaPhase != RoombaPhase.STOP:
                    return "El roomba no esta pausado, no se puede reactivar."

                await cls.SendRoombaOrder(RoombaAction.RESUME, roombaTarget)

                return "Reactivando el roomba."
            except Exception as ex:
                return f"Error al reactivar el roomba: {ex}"
    # endregion

    # region _buildPayload
    @staticmethod
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

    # region _getRoombaRoomsIds
    @staticmethod
    def _getRoombaRoomsIds(roombaTarget: RoombaTarget) -> list[str]:
        match roombaTarget:
            case RoombaTarget.KITCHEN: return ["11"]
            case RoombaTarget.DIEGO: return ["16"]
            case RoombaTarget.MARCOS: return ["21"]
            case RoombaTarget.KITCHEN_AND_GRANDMOTHER: return ["4", "11"]
            case RoombaTarget.BEDROOMS: return ["21", "16"]
            case RoombaTarget.BEDROOM_AND_TOILET: return ["23", "25"]
            case RoombaTarget.HALLWAY_AND_TOILET: return ["19", "22", "24"]
            case RoombaTarget.LIVING_ROOM: return ["1", "18"]
            case _: return []
    # endregion

    # region _parsePhase
    @staticmethod
    def _parsePhase(phase: str | None) -> RoombaPhase | None:
        match phase:
            case "charge": return RoombaPhase.CHARGE
            case "run": return RoombaPhase.RUN
            case "stop": return RoombaPhase.STOP
            case "hmUsrDock": return RoombaPhase.HM_USR_DOCK
            case "stuck": return RoombaPhase.STUCK
            case _: return None
    # endregion
