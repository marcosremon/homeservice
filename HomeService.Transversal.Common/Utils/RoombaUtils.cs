using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Nodes;
using HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;
using HomeService.Application.Event;
using HomeService.Domain.Model.Enum.Roomba;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Common.Roomba;
using HomeService.Transversal.Common.Roomba.Payload;
using MQTTnet;
using MQTTnet.Formatter;

namespace HomeService.Transversal.Common.Utils;

public static class RoombaUtils
{
    #region Properties
    private static readonly SemaphoreSlim RoombaLock = new SemaphoreSlim(1, 1);

    // Ventana en la que se permite el arranque automatico.
    private static readonly TimeOnly AutoStartFrom = new TimeOnly(8, 0);
    private static readonly TimeOnly AutoStartTo = new TimeOnly(21, 0);

    // Espera maxima a que la roomba confirme la orden (phase: run).
    private const double CommandConfirmationTimeoutSeconds = 8.0;

    // Pausa entre PAUSE y DOCK al mandarla a casa.
    private const double PauseBeforeDockSeconds = 3.0;

    // Margen para que el shadow nos diga el pmap_id antes de mandar la orden.
    private const double PmapDiscoverySeconds = 3.0;

    // La roomba solo acepta un cliente MQTT y tarda ~2,5s en volver a escuchar tras
    // cerrar una conexion: sin reintentos, la segunda orden seguida se come un
    // ECONNREFUSED.
    private const int ConnectRetryAttempts = 5;
    private const double ConnectRetrySeconds = 2.5;

    // Espera maxima a que el shadow conteste con la fase actual.
    private const double PhaseQueryTimeoutSeconds = 5.0;

    // Lo que escucha GetRoomInfo antes de rendirse.
    private const double RoomInfoListenSeconds = 30.0;
    #endregion

    #region BuildClientOptions
    // Cliente MQTT contra la roomba: TLS 1.2 sin validar el certificado. El del
    // aparato es autofirmado y su firmware solo habla TLS 1.2.
    private static MqttClientOptions BuildClientOptions(Settings settings) =>
        new MqttClientOptionsBuilder()
            .WithTcpServer(settings.RoombaId, int.TryParse(settings.RoombaPort, out int roombaPort) ? roombaPort : 8883)
            .WithCredentials(settings.RoombaBlid, settings.RoombaPasswd)
            .WithClientId(settings.RoombaBlid)
            .WithProtocolVersion(MqttProtocolVersion.V311)
            .WithTlsOptions(tlsOptions => tlsOptions
                .UseTls()
                .WithSslProtocols(SslProtocols.Tls12)
                .WithCertificateValidationHandler(_ => true))
            .Build();
    #endregion

    #region Connect
    // Conecta reintentando: tras cerrar una sesion la roomba rechaza un rato.
    private static async Task<IMqttClient> Connect(Settings settings)
    {
        MqttClientFactory mqttClientFactory = new MqttClientFactory();
        Exception lastError = new InvalidOperationException("sin intentos");

        for (int attempt = 0; attempt < ConnectRetryAttempts; attempt++)
        {
            IMqttClient mqttClient = mqttClientFactory.CreateMqttClient();

            try
            {
                await mqttClient.ConnectAsync(BuildClientOptions(settings), CancellationToken.None);
                return mqttClient;
            }
            catch (Exception ex)
            {
                lastError = ex;
                mqttClient.Dispose();

                if (attempt < ConnectRetryAttempts - 1)
                    await Task.Delay(TimeSpan.FromSeconds(ConnectRetrySeconds));
            }
        }

        throw new InvalidOperationException($"no se pudo conectar tras {ConnectRetryAttempts} intentos: {lastError.Message}");
    }
    #endregion

    #region ApplyStateMessage
    // Vuelca un mensaje del shadow sobre el estado que vamos acumulando. La roomba
    // manda el estado unas veces plano y otras dentro de state.reported, de ahi el
    // desdoble.
    private static void ApplyStateMessage(string rawPayload, RoombaState roombaState, RoombaTarget? roombaTarget, RoombaAction? roombaAction = null)
    {
        try
        {
            JsonNode? root = JsonNode.Parse(rawPayload);
            if (root is null)
                return;

            JsonNode stateNode = root["state"]?["reported"] ?? root;

            if (ReadString(stateNode["pmapId"]) is string pmapId && !GeneralUtils.IsNullOrEmpty(pmapId))
                roombaState.PmapId = pmapId;

            if (ReadString(stateNode["userPmapvId"]) is string userPmapvId && !GeneralUtils.IsNullOrEmpty(userPmapvId))
                roombaState.PmapVersion = userPmapvId;

            if (ReadInt(stateNode["batPct"]) is int battery)
                roombaState.Battery = battery;

            if (stateNode["bin"] is JsonObject binNode && ReadBool(binNode["full"]) is bool binFull)
                roombaState.BinFull = binFull;

            if (stateNode["cleanMissionStatus"] is not JsonObject missionNode)
                return;

            // El error se vuelca antes que la fase: un mismo mensaje puede traer
            // "phase": "run" y un "error", y el evento de activacion tiene que salir
            // con el error de este mensaje, no con el del anterior.
            if (ReadInt(missionNode["error"]) is int errorCode)
            {
                roombaState.ErrorCode = errorCode;
                roombaState.ErrorMessage = errorCode == 0
                    ? string.Empty
                    : $"El roomba reporto error: codigo {errorCode}";
            }

            if (missionNode["phase"] is null)
                return;

            RoombaPhase? phase = ParsePhase(ReadString(missionNode["phase"]));

            if (phase is not null)
            {
                roombaState.Phase = phase.Value;
                roombaState.PhaseSeen = true;
            }

            // Solo un START cuenta como activacion: al pausar o mandarla a casa el
            // shadow sigue diciendo "run" un rato, y eso machacaba
            // LastRoombaActivation con la hora de la pausa.
            if (phase == RoombaPhase.RUN && !roombaState.CommandAccepted && roombaAction == RoombaAction.START)
            {
                roombaState.CommandAccepted = true;
                roombaState.ActivationEvents.Add(BuildActivationRequest(
                    RoombaPhase.RUN,
                    roombaTarget,
                    isActivation: true,
                    batteryPercent: roombaState.Battery,
                    binFull: roombaState.BinFull,
                    errorCode: roombaState.ErrorCode,
                    errorMessage: roombaState.ErrorMessage,
                    pmapId: roombaState.PmapId,
                    userPmapvId: roombaState.PmapVersion));
            }

            // Vuelta a la base o a cargar: la mision ha terminado.
            if (phase is (RoombaPhase.CHARGE or RoombaPhase.HM_USR_DOCK) && !roombaState.FinishSeen)
            {
                roombaState.FinishSeen = true;
                roombaState.ActivationEvents.Add(BuildActivationRequest(
                    phase.Value,
                    roombaTarget,
                    isFinished: true,
                    batteryPercent: roombaState.Battery,
                    binFull: roombaState.BinFull,
                    errorCode: roombaState.ErrorCode,
                    errorMessage: roombaState.ErrorMessage,
                    pmapId: roombaState.PmapId,
                    userPmapvId: roombaState.PmapVersion));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RoombaUtils -> error parseando mensaje MQTT: {ex.Message}");
        }
    }
    #endregion

    #region GetRoomInfo
    // Vuelca por consola todo lo que dice la roomba durante 30 segundos. Solo sirve
    // para averiguar los region_id a mano cuando cambias el mapa: mueve la roomba
    // desde la app de iRobot mientras esto escucha.
    public static async Task GetRoomInfo()
    {
        Settings settings = Settings.GetSettings();

        if (GeneralUtils.IsNullOrEmpty(settings.RoombaId) || GeneralUtils.IsNullOrEmpty(settings.RoombaBlid))
        {
            Console.WriteLine("RoombaUtils -> GetRoomInfo -> Configuracion incompleta (ip o blid vacios)");
            return;
        }

        try
        {
            using IMqttClient mqttClient = await Connect(settings);

            mqttClient.ApplicationMessageReceivedAsync += eventArgs =>
            {
                string rawPayload = eventArgs.ApplicationMessage.ConvertPayloadToString() ?? string.Empty;

                Console.WriteLine($"{Environment.NewLine}=== MENSAJE RAW ==={Environment.NewLine}{rawPayload}{Environment.NewLine}==================");

                if (rawPayload.Contains("regions") || rawPayload.Contains("pmapId") || rawPayload.Contains("pmap"))
                    Console.WriteLine("^^^ CONTIENE INFO DE MAPA ^^^");

                return Task.CompletedTask;
            };

            await mqttClient.SubscribeAsync($"$aws/things/{settings.RoombaBlid}/shadow/update");
            await mqttClient.SubscribeAsync("wifistat");
            await mqttClient.SubscribeAsync("#");

            Console.WriteLine($"Escuchando {RoomInfoListenSeconds:F0} segundos... mueve el roomba desde la app para forzar mensajes");

            await Task.Delay(TimeSpan.FromSeconds(RoomInfoListenSeconds));
            await mqttClient.DisconnectAsync();

            Console.WriteLine("RoombaUtils -> GetRoomInfo -> fin de la escucha");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RoombaUtils -> GetRoomInfo -> {ex.Message}");
        }
    }
    #endregion

    #region GetRoombaPhase
    // Fase actual de la roomba, o null si no se puede consultar. null significa "no se
    // pudo preguntar" (aparato apagado, red caida, timeout); todas las ordenes de
    // abajo lo tratan como motivo para abortar.
    public static async Task<RoombaPhase?> GetRoombaPhase()
    {
        Settings settings = Settings.GetSettings();

        if (GeneralUtils.IsNullOrEmpty(settings.RoombaId) || GeneralUtils.IsNullOrEmpty(settings.RoombaBlid))
        {
            Console.WriteLine("RoombaUtils -> GetRoombaPhase -> Configuracion incompleta (ip o blid vacios)");
            return null;
        }

        RoombaState roombaState = new RoombaState();

        try
        {
            using IMqttClient mqttClient = await Connect(settings);

            mqttClient.ApplicationMessageReceivedAsync += eventArgs =>
            {
                ApplyStateMessage(eventArgs.ApplicationMessage.ConvertPayloadToString() ?? string.Empty, roombaState, null);
                return Task.CompletedTask;
            };

            await mqttClient.SubscribeAsync($"$aws/things/{settings.RoombaBlid}/shadow/update");

            double elapsed = 0.0;
            while (!roombaState.PhaseSeen && elapsed < PhaseQueryTimeoutSeconds)
            {
                await Task.Delay(250);
                elapsed += 0.25;
            }

            await mqttClient.DisconnectAsync();

            if (!roombaState.PhaseSeen)
                Console.WriteLine("RoombaUtils -> GetRoombaPhase -> la roomba no contesto a tiempo");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RoombaUtils -> GetRoombaPhase -> {ex.Message}");
        }

        return roombaState.PhaseSeen ? roombaState.Phase : null;
    }
    #endregion

    #region SendRoombaOrder
    public static async Task SendRoombaOrder(RoombaAction roombaAction, RoombaTarget? roombaTarget = null)
    {
        Settings settings = Settings.GetSettings();

        if (GeneralUtils.IsNullOrEmpty(settings.RoombaId) ||
            GeneralUtils.IsNullOrEmpty(settings.RoombaBlid) ||
            GeneralUtils.IsNullOrEmpty(settings.RoombaPasswd))
        {
            Console.WriteLine("RoombaUtils -> SendRoombaOrder -> Configuracion incompleta (ip, blid o pass vacios)");
            return;
        }

        List<string> roomIds = roombaTarget is not null ? GetRoombaRoomsIds(roombaTarget.Value) : [];
        RoombaState roombaState = new RoombaState();

        try
        {
            using IMqttClient mqttClient = await Connect(settings);

            mqttClient.ApplicationMessageReceivedAsync += eventArgs =>
            {
                ApplyStateMessage(eventArgs.ApplicationMessage.ConvertPayloadToString() ?? string.Empty, roombaState, roombaTarget, roombaAction);
                return Task.CompletedTask;
            };

            await mqttClient.SubscribeAsync($"$aws/things/{settings.RoombaBlid}/shadow/update");
            await mqttClient.SubscribeAsync("wifistat");

            // La roomba anuncia su mapa nada mas conectar; hay que darle un momento
            // antes de mandar una orden por regiones.
            await Task.Delay(TimeSpan.FromSeconds(PmapDiscoverySeconds));

            string finalPmapId = GeneralUtils.IsNullOrEmpty(roombaState.PmapId) ? settings.RoombaPmapId : roombaState.PmapId;
            string finalPmapVersion = GeneralUtils.IsNullOrEmpty(roombaState.PmapVersion) ? settings.RoombaPmapVersion : roombaState.PmapVersion;

            if (GeneralUtils.IsNullOrEmpty(finalPmapId))
            {
                Console.WriteLine("RoombaUtils -> SendRoombaOrder -> No se pudo obtener pmap_id. Abortando.");
                return;
            }

            Console.WriteLine($"  -> Usando pmap_id: {finalPmapId} / version: {finalPmapVersion}");
            Console.WriteLine($"  -> Regiones: [{string.Join(", ", roomIds)}]");

            RoombaPayload roombaPayload = BuildPayload(roombaAction, roomIds, finalPmapId, finalPmapVersion);

            await mqttClient.PublishStringAsync(
                $"cmd/{settings.RoombaBlid}/delta",
                JsonSerializer.Serialize(roombaPayload, JsonModelConfig.Default));

            Console.WriteLine("  -> Orden enviada, esperando confirmacion...");

            double elapsed = 0.0;
            while (!roombaState.CommandAccepted && elapsed < CommandConfirmationTimeoutSeconds)
            {
                await Task.Delay(500);
                elapsed += 0.5;
            }

            // El evento de activacion lo arma el handler en cuanto ve phase "run";
            // aqui solo se publica, para que no salga desde una tarea que puede morir
            // con la conexion.
            foreach (PatchRoombaStateRequest activationEvent in roombaState.ActivationEvents)
                RoombaActivatedEvent.Publish(activationEvent);

            RoombaActivatedEvent.Publish(BuildActivationRequest(
                roombaState.Phase,
                roombaTarget,
                isFinished: roombaState.Phase is (RoombaPhase.CHARGE or RoombaPhase.HM_USR_DOCK) && !roombaState.FinishSeen,
                batteryPercent: roombaState.Battery,
                binFull: roombaState.BinFull,
                errorCode: roombaState.ErrorCode,
                errorMessage: roombaState.ErrorMessage,
                pmapId: finalPmapId,
                userPmapvId: finalPmapVersion));

            Console.WriteLine(roombaState.CommandAccepted
                ? "  -> roomba confirmo inicio de limpieza (phase: run)"
                : "  -> La roomba no confirmo la orden dentro del margen");

            await mqttClient.DisconnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"RoombaUtils -> SendRoombaOrder -> {ex.Message}");
        }
    }
    #endregion

    #region BuildActivationRequest
    public static PatchRoombaStateRequest BuildActivationRequest(
        RoombaPhase roombaPhase,
        RoombaTarget? roombaTarget = null,
        bool isActivation = false,
        bool isFinished = false,
        int batteryPercent = 0,
        bool binFull = false,
        int errorCode = 0,
        string errorMessage = "",
        string pmapId = "",
        string userPmapvId = "") => new PatchRoombaStateRequest()
        {
            IsActivation = isActivation,
            IsFinished = isFinished,
            Target = roombaTarget ?? RoombaTarget.FULL_HOUSE,
            Phase = roombaPhase,
            BatteryPercent = batteryPercent,
            BinFull = binFull,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            PmapId = pmapId,
            UserPmapvId = userPmapvId,
            IsOnline = true,
        };
    #endregion

    #region StartRoombaIfHouseIsEmpty
    public static async Task StartRoombaIfHouseIsEmpty(DateTime lastRoombaActivation)
    {
        await RoombaLock.WaitAsync();

        try
        {
            DateTime now = DateTime.Now;
            TimeOnly nowTime = TimeOnly.FromDateTime(now);

            bool isValidTime = nowTime >= AutoStartFrom && nowTime <= AutoStartTo;
            bool isActivatedToday = lastRoombaActivation.Date == now.Date;

            if (isActivatedToday)
            {
                Console.WriteLine("roomba ya se activo hoy, omitiendo.");
                return;
            }

            if (!isValidTime)
            {
                Console.WriteLine($"Fuera de horario ({AutoStartFrom:HH:mm} - {AutoStartTo:HH:mm}), omitiendo.");
                return;
            }

            RoombaPhase? roombaPhase = await GetRoombaPhase();

            if (roombaPhase is null)
            {
                Console.WriteLine("No se pudo obtener el estado del roomba, abortando.");
                return;
            }

            if (roombaPhase == RoombaPhase.STUCK)
            {
                Console.WriteLine("roomba atascado, revisalo manualmente.");
                return;
            }

            if (roombaPhase is RoombaPhase.RUN or RoombaPhase.HM_USR_DOCK)
            {
                Console.WriteLine($"StartRoombaIfHouseIsEmpty ignorado - fase actual: {roombaPhase}");
                return;
            }

            await SendRoombaOrder(RoombaAction.START, RoombaTarget.FULL_HOUSE);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en RoombaUtils -> StartRoombaIfHouseIsEmpty: {ex.Message}");
        }
        finally
        {
            RoombaLock.Release();
        }
    }
    #endregion

    #region StartRoomba
    public static async Task<string> StartRoomba(RoombaTarget roombaTarget)
    {
        await RoombaLock.WaitAsync();

        try
        {
            RoombaPhase? roombaPhase = await GetRoombaPhase();

            if (roombaPhase is null)
                return "No se pudo obtener el estado del roomba.";

            if (roombaPhase == RoombaPhase.STUCK)
                return "El roomba esta atascado, revisalo manualmente.";

            if (roombaPhase is RoombaPhase.RUN or RoombaPhase.HM_USR_DOCK)
                return "El roomba ya esta limpiando.";

            await SendRoombaOrder(RoombaAction.START, roombaTarget);

            if (roombaTarget == RoombaTarget.FULL_HOUSE)
                return "Iniciando limpieza de la casa completa.";

            return $"Iniciando limpieza de {roombaTarget.ToString().ToLowerInvariant().Replace('_', ' ')}.";
        }
        catch (Exception ex)
        {
            return $"Error al iniciar el roomba: {ex.Message}";
        }
        finally
        {
            RoombaLock.Release();
        }
    }
    #endregion

    #region SendRoombaHome
    public static async Task<string> SendRoombaHome()
    {
        await RoombaLock.WaitAsync();

        try
        {
            RoombaPhase? roombaPhase = await GetRoombaPhase();

            if (roombaPhase is null)
                return "No se pudo obtener el estado del roomba.";

            if (roombaPhase == RoombaPhase.STUCK)
                return "El roomba esta atascado, revisalo manualmente.";

            if (roombaPhase is RoombaPhase.CHARGE or RoombaPhase.HM_USR_DOCK)
                return "El roomba ya esta en casa.";

            // La roomba ignora el DOCK si viene sin pausa previa.
            await SendRoombaOrder(RoombaAction.PAUSE);
            await Task.Delay(TimeSpan.FromSeconds(PauseBeforeDockSeconds));
            await SendRoombaOrder(RoombaAction.DOCK);

            return "Enviando el roomba a casa.";
        }
        catch (Exception ex)
        {
            return $"Error al enviar el roomba a casa: {ex.Message}";
        }
        finally
        {
            RoombaLock.Release();
        }
    }
    #endregion

    #region PauseRoomba
    public static async Task<string> PauseRoomba()
    {
        await RoombaLock.WaitAsync();

        try
        {
            RoombaPhase? roombaPhase = await GetRoombaPhase();

            if (roombaPhase is null)
                return "No se pudo obtener el estado del roomba.";

            if (roombaPhase == RoombaPhase.STUCK)
                return "El roomba esta atascado, revisalo manualmente.";

            if (roombaPhase != RoombaPhase.RUN)
                return "El roomba no esta limpiando ahora mismo.";

            await SendRoombaOrder(RoombaAction.PAUSE);

            return "Pausando el roomba.";
        }
        catch (Exception ex)
        {
            return $"Error al pausar el roomba: {ex.Message}";
        }
        finally
        {
            RoombaLock.Release();
        }
    }
    #endregion

    #region ReactiveRoomba
    public static async Task<string> ReactiveRoomba(RoombaTarget roombaTarget)
    {
        await RoombaLock.WaitAsync();

        try
        {
            RoombaPhase? roombaPhase = await GetRoombaPhase();

            if (roombaPhase is null)
                return "No se pudo obtener el estado del roomba.";

            if (roombaPhase == RoombaPhase.STUCK)
                return "El roomba esta atascado, revisalo manualmente.";

            if (roombaPhase != RoombaPhase.STOP)
                return "El roomba no esta pausado, no se puede reactivar.";

            await SendRoombaOrder(RoombaAction.RESUME, roombaTarget);

            return "Reactivando el roomba.";
        }
        catch (Exception ex)
        {
            return $"Error al reactivar el roomba: {ex.Message}";
        }
        finally
        {
            RoombaLock.Release();
        }
    }
    #endregion

    #region BuildPayload
    private static RoombaPayload BuildPayload(RoombaAction roombaAction, List<string> roomIds, string pmapId, string pmapVersion)
    {
        string command = roombaAction.ToString().ToLowerInvariant();
        long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (roombaAction != RoombaAction.START || roomIds.Count == 0)
            return new RoombaPayload { Command = command, Time = unixTime, Initiator = "rmtApp" };

        return new RoombaPayload
        {
            Command = command,
            Time = unixTime,
            Initiator = "rmtApp",
            Ordered = 1,
            PmapId = pmapId,
            UserPmapvId = pmapVersion,
            Regions = [.. roomIds.Select(roomId => new RoombaRegion
            {
                RegionId = roomId,
                Type = "rid",
                Params = new RoombaRegionParams(),
            })],
        };
    }
    #endregion

    #region GetRoombaRoomsIds
    private static List<string> GetRoombaRoomsIds(RoombaTarget roombaTarget) => roombaTarget switch
    {
        RoombaTarget.KITCHEN => ["11"],
        RoombaTarget.DIEGO => ["16"],
        RoombaTarget.MARCOS => ["21"],
        RoombaTarget.KITCHEN_AND_GRANDMOTHER => ["4", "11"],
        RoombaTarget.BEDROOMS => ["21", "16"],
        RoombaTarget.BEDROOM_AND_TOILET => ["23", "25"],
        RoombaTarget.HALLWAY_AND_TOILET => ["19", "22", "24"],
        RoombaTarget.LIVING_ROOM => ["1", "18"],
        _ => []
    };
    #endregion

    #region ParsePhase
    private static RoombaPhase? ParsePhase(string? phase) => phase switch
    {
        "charge" => RoombaPhase.CHARGE,
        "run" => RoombaPhase.RUN,
        "stop" => RoombaPhase.STOP,
        "hmUsrDock" => RoombaPhase.HM_USR_DOCK,
        "stuck" => RoombaPhase.STUCK,
        _ => null
    };
    #endregion

    #region ReadString / ReadInt / ReadBool
    private static string? ReadString(JsonNode? node) =>
        node?.GetValueKind() == JsonValueKind.String ? node.GetValue<string>() : null;

    private static int? ReadInt(JsonNode? node) =>
        node?.GetValueKind() == JsonValueKind.Number ? node.GetValue<int>() : null;

    private static bool? ReadBool(JsonNode? node) =>
        node?.GetValueKind() is JsonValueKind.True or JsonValueKind.False ? node.GetValue<bool>() : null;
    #endregion
}
