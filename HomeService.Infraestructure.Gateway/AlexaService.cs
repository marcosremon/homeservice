using HomeService.Application.DataTransferObject.Alexa;
using HomeService.Application.DataTransferObject.Gemini;
using HomeService.Application.Interface.Service;
using HomeService.Domain.Model.Enum.Alexa;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.Wrapper.Alexa.AlexaRequest;
using System.Text.Json;

namespace HomeService.Infraestructure.Gateway
{
    public class AlexaService : IAlexaService
    {
        private const string _historyAttribute = "history";

        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly ILightService _lightService;
        private readonly IRoombaService _roombaService;
        private readonly IGeminiService _geminiService;
        private readonly IComputerStatusService _computerStatusService;
        private readonly ITemperatureSensorService _temperatureSensorService;
        private readonly Settings _settings;

        public AlexaService(
            ILightService lightService,
            IRoombaService roombaService,
            IGeminiService geminiService,
            IComputerStatusService computerStatusService,
            ITemperatureSensorService temperatureSensorService,
            Settings settings)
        {
            _lightService = lightService;
            _roombaService = roombaService;
            _geminiService = geminiService;
            _computerStatusService = computerStatusService;
            _temperatureSensorService = temperatureSensorService;
            _settings = settings;
        }

        #region SendAlexaOrder
        public async Task<AlexaResponse> SendAlexaOrder(AlexaRequest alexaRequest)
        {
            AlexaResponse alexaResponse = new AlexaResponse();

            try
            {
                AlexaIntent? intent = alexaRequest.AlexaRequestData.Intent;
                string rawIntentName = intent?.Name ?? string.Empty;
                string speechText = string.Empty;
                bool keepSessionOpen = false;

                bool isLaunchRequest = GeneralUtils.ParseEnumExact<AlexaRequestType>(alexaRequest.AlexaRequestData.Type) == AlexaRequestType.LaunchRequest;
                IntentName? intentName = _resolveIntentName(rawIntentName);

                if (isLaunchRequest)
                {
                    speechText = "Hola, ya puedes hablar conmigo. ¿Qué quieres saber?";
                    keepSessionOpen = true;
                }
                else if (intentName == IntentName.ConversationIntent)
                {
                    AlexaSlot? querySlot = null;
                    intent?.Slots?.TryGetValue("query", out querySlot);
                    string userText = querySlot?.Value ?? string.Empty;

                    if (GeneralUtils.IsNullOrWhiteSpace(userText))
                    {
                        speechText = "No te he entendido, ¿puedes repetir?";
                        keepSessionOpen = true;
                    }
                    else
                    {
                        GeminiTurnRequest geminiTurnRequest = new GeminiTurnRequest
                        {
                            UserText = userText,
                            History = _readHistory(alexaRequest),
                        };

                        GeminiTurnResponse geminiTurnResponse = await _geminiService.Converse(geminiTurnRequest);
                        _writeHistory(alexaResponse, geminiTurnResponse.UpdatedHistory);

                        speechText = geminiTurnResponse.Replay;
                        keepSessionOpen = true;
                    }
                }
                else if (intentName is not null)
                {
                    string message = intentName switch
                    {
                        IntentName.computer_status_order_ => await _computerStatusService.ExecuteComputerStatusOrder(rawIntentName),
                        IntentName.roomba_order_ => await _roombaService.ExecuteRoombaOrder(rawIntentName, alexaRequest),
                        IntentName.light_order_ => await _lightService.ExecuteLightOrder(rawIntentName),
                        IntentName.temperature_sensor_order_ => await _temperatureSensorService.ExecuteTemperatureSensorOrder(rawIntentName, alexaRequest),
                        _ => string.Empty,
                    };

                    alexaResponse.AlexaResponseContent = AlexaUtils.BuildAlexaResponse(rawIntentName, message);
                }
                else
                {
                    Console.WriteLine($"AlexaService -> intent desconocido: {rawIntentName}");
                    speechText = "No te he entendido, ¿puedes repetir?";
                    keepSessionOpen = true;
                }

                alexaResponse.Version = _settings.AlexaVersion;

                if (keepSessionOpen)
                    alexaResponse.AlexaResponseContent = AlexaUtils.BuildConversationResponse(speechText, keepSessionOpen);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error on AlexaService -> SendAlexaOrder -> {ex.Message}");
            }

            return alexaResponse;
        }
        #endregion

        #region _resolveIntentName
        // Alexa manda el nombre completo del intent y el enum guarda el prefijo:
        // "ConversationIntent" coincide letra a letra, pero los intents de dominio
        // llegan como "<prefijo><orden>" (p. ej. "light_order_encender_luces_salon"),
        // asi que ahi se resuelve por prefijo.
        private static IntentName? _resolveIntentName(string rawIntentName)
        {
            if (GeneralUtils.IsNullOrEmpty(rawIntentName))
                return null;

            IntentName? exactIntentName = GeneralUtils.ParseEnumExact<IntentName>(rawIntentName);
            if (exactIntentName is not null)
                return exactIntentName;

            foreach (IntentName intentName in Enum.GetValues<IntentName>())
            {
                string prefix = intentName.ToString();

                if (prefix.EndsWith('_') && rawIntentName.StartsWith(prefix, StringComparison.Ordinal))
                    return intentName;
            }

            return null;
        }
        #endregion

        #region _readHistory
        // El historial de la conversacion viaja en los sessionAttributes: alexa
        // devuelve en cada turno lo que se le mando en el anterior, asi que es el
        // unico "estado" que tiene la skill.
        private static List<GeminiTurn> _readHistory(AlexaRequest alexaRequest)
        {
            try
            {
                if (alexaRequest.Session?.Attributes is not null &&
                    alexaRequest.Session.Attributes.TryGetValue(_historyAttribute, out string? historyJson) &&
                    !GeneralUtils.IsNullOrEmpty(historyJson))
                {
                    return JsonSerializer.Deserialize<List<GeminiTurn>>(historyJson, _jsonOptions) ?? [];
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[History read] {ex.Message}");
            }

            return [];
        }
        #endregion

        #region _writeHistory
        private static void _writeHistory(AlexaResponse alexaResponse, List<GeminiTurn> history)
        {
            try
            {
                alexaResponse.SessionAttributes = new Dictionary<string, string>
                {
                    [_historyAttribute] = JsonSerializer.Serialize(history, _jsonOptions),
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[History write] {ex.Message}");
            }
        }
        #endregion
    }
}
