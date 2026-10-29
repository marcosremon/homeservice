using HomeService.Application.DataTransferObject.Gemini;
using HomeService.Application.Interface.Service;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Common.Utils;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace HomeService.Infraestructure.Gateway
{
    public class GeminiService : IGeminiService
    {
        // Presupuesto de caracteres del historial reenviado en sessionAttributes: la
        // respuesta de alexa entera no puede pasar de 24KB (voz + historial).
        private const int _historyCharBudget = 10000;

        // alexa rechaza un outputSpeech de mas de 8000 caracteres.
        private const int _maxSpeechLength = 7900;

        // Eres un asistente por voz en un altavoz inteligente. Responde siempre en
        // espanol, de forma natural y conversacional. Ajusta la longitud a la
        // pregunta. No uses listas, markdown ni simbolos: tu texto se lee en voz alta.
        private const string _systemPrompt =
            "You are a voice assistant on a smart speaker. " +
            "Always respond in Spanish, in a natural and conversational tone. " +
            "Adjust the length of your response to the question: be brief for simple matters (1 or 2 sentences) " +
            "and elaborate more when the topic requires it or the user asks for details. " +
            "Do not use lists, markdown, or symbols: your text will be read aloud, " +
            "so write in continuous, flowing sentences.";

        // Justo por debajo del corte de 8 segundos de alexa.
        private static readonly TimeSpan _requestTimeout = TimeSpan.FromSeconds(8);

        // La API de Gemini espera camelCase y sin los opcionales a null.
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Settings _settings;

        public GeminiService(IHttpClientFactory httpClientFactory, Settings settings)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings;
        }

        #region Converse
        public async Task<GeminiTurnResponse> Converse(GeminiTurnRequest geminiTurnRequest)
        {
            geminiTurnRequest.History.Add(new GeminiTurn { Role = "user", Text = geminiTurnRequest.UserText });

            GeminiPayload geminiPayload = new GeminiPayload
            {
                SystemInstruction = new SystemInstruction { Parts = [new GeminiPart { Text = _systemPrompt }] },
                Contents = geminiTurnRequest.History
                    .Select(turn => new GeminiContent { Role = turn.Role, Parts = [new GeminiPart { Text = turn.Text }] })
                    .ToList(),
                GenerationConfig = new GenerationConfig
                {
                    MaxOutputTokens = 1024,                                     // permite respuestas largas (~2-3 min de voz)
                    Temperature = 0.7,
                    ThinkingConfig = new ThinkingConfig { ThinkingBudget = 0 }, // sin "pensar": rapido, cabe en los 8s de alexa
                },
            };

            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_settings.GeminiModel}:generateContent";
            string reply;

            try
            {
                using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, url);
                httpRequestMessage.Content = new StringContent(JsonSerializer.Serialize(geminiPayload, _jsonOptions), Encoding.UTF8, "application/json");

                // La clave va en cabecera, no en la query: asi no acaba escrita en los
                // logs de acceso ni en el historial de proxies.
                httpRequestMessage.Headers.Add("x-goog-api-key", _settings.GeminiApiKey);

                using CancellationTokenSource timeout = new CancellationTokenSource(_requestTimeout);
                HttpClient httpClient = _httpClientFactory.CreateClient();
                using HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, timeout.Token);

                string body = await httpResponseMessage.Content.ReadAsStringAsync(timeout.Token);

                if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
                {
                    Console.WriteLine($"[Gemini] HTTP {(int)httpResponseMessage.StatusCode}: {body}");
                    reply = "Lo siento, no he podido pensar la respuesta ahora mismo.";
                }
                else
                {
                    reply = _extractText(body);
                }
            }
            catch (OperationCanceledException)
            {
                reply = "Me ha llevado demasiado, ¿puedes repetirlo?";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Gemini] Error: {ex.Message}");
                reply = "Ha habido un problema al responder.";
            }

            geminiTurnRequest.History.Add(new GeminiTurn { Role = "model", Text = reply });
            _trimHistory(geminiTurnRequest.History);

            return new GeminiTurnResponse
            {
                Replay = reply,
                UpdatedHistory = geminiTurnRequest.History,
            };
        }
        #endregion

        #region _trimHistory
        // Recorta los turnos mas antiguos hasta caber en el presupuesto, y se asegura
        // de que el historial empiece por un turno "user": Gemini rechaza una
        // conversacion que arranque con "model".
        private static void _trimHistory(List<GeminiTurn> history)
        {
            int total = history.Sum(turn => turn.Text.Length);

            while (history.Count > 2 && total > _historyCharBudget)
            {
                total -= history[0].Text.Length;
                history.RemoveAt(0);
            }

            while (history.Count > 0 && history[0].Role == "model")
                history.RemoveAt(0);
        }
        #endregion

        #region _extractText
        private static string _extractText(string body)
        {
            JsonArray? candidates = JsonNode.Parse(body)?["candidates"]?.AsArray();

            if (candidates is not null && candidates.Count > 0)
            {
                JsonNode? candidate = candidates[0];
                string? finishReason = candidate?["finishReason"]?.GetValue<string>();
                JsonArray? parts = candidate?["content"]?["parts"]?.AsArray();

                StringBuilder textBuilder = new StringBuilder();
                if (parts is not null)
                    foreach (JsonNode? part in parts)
                        textBuilder.Append(part?["text"]?.GetValue<string>());

                string text = textBuilder.ToString().Trim();

                if (text.Length > _maxSpeechLength)
                    text = text[.._maxSpeechLength];

                if (!GeneralUtils.IsNullOrEmpty(text))
                    return text;

                Console.WriteLine($"[Gemini] sin texto (finishReason={finishReason}): {body}");
            }

            return "No tengo una respuesta para eso.";
        }
        #endregion
    }
}
