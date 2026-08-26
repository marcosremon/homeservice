import json
from typing import Any

import httpx
from pydantic import TypeAdapter

from application.data_transfer_object.gemini.GeminiContent import GeminiContent
from application.data_transfer_object.gemini.GeminiPart import GeminiPart
from application.data_transfer_object.gemini.GeminiPayload import GeminiPayload
from application.data_transfer_object.gemini.GeminiTurn import GeminiTurn
from application.data_transfer_object.gemini.GeminiTurnRequest import GeminiTurnRequest
from application.data_transfer_object.gemini.GeminiTurnResponse import GeminiTurnResponse
from application.data_transfer_object.gemini.GenerationConfig import GenerationConfig
from application.data_transfer_object.gemini.SystemInstruction import SystemInstruction
from application.data_transfer_object.gemini.ThinkingConfig import ThinkingConfig
from application.interface.service.IGeminiService import IGeminiService
from transversal.common.configuration.Settings import Settings
from transversal.common.utils.GeneralUtils import GeneralUtils

# Presupuesto de caracteres del historial reenviado en sessionAttributes.
# La respuesta de Alexa entera no puede pasar de 24KB (incluye voz + historial).
_HISTORY_CHAR_BUDGET: int = 10000

# Alexa rechaza un outputSpeech de mas de 8000 caracteres.
_MAX_SPEECH_LENGTH: int = 7900

# Justo por debajo del corte de Alexa.
_REQUEST_TIMEOUT_SECONDS: float = 8.0

# Eres un asistente por voz en un altavoz inteligente. Responde siempre en espanol, de forma natural y conversacional.
# Ajusta la longitud a la pregunta: se breve en lo sencillo (1 o 2 frases) y desarrolla mas cuando el tema lo pida.
# No uses listas, markdown ni simbolos: tu texto se lee en voz alta, asi que escribe en frases corridas.
_SYSTEM_PROMPT: str = (
    "You are a voice assistant on a smart speaker. "
    "Always respond in Spanish, in a natural and conversational tone. "
    "Adjust the length of your response to the question: be brief for simple matters (1 or 2 sentences) "
    "and elaborate more when the topic requires it or the user asks for details. "
    "Do not use lists, markdown, or symbols: your text will be read aloud, "
    "so write in continuous, flowing sentences."
)

# Los nombres de los campos ya van en camelCase, que es justo lo que espera la API
# de Gemini; exclude_none quita los opcionales, como el DefaultIgnoreCondition de C#.
_payloadAdapter: TypeAdapter[GeminiPayload] = TypeAdapter(GeminiPayload)

class GeminiService(IGeminiService):

    def __init__(self, httpClient: httpx.AsyncClient, settings: Settings):
        self._httpClient = httpClient
        self._settings = settings

    # region Converse
    async def Converse(self, geminiTurnRequest: GeminiTurnRequest) -> GeminiTurnResponse:
        geminiTurnRequest.history.append(GeminiTurn(role = "user", text = geminiTurnRequest.userText))

        payload: GeminiPayload = GeminiPayload(
            systemInstruction = SystemInstruction(parts = [GeminiPart(text = _SYSTEM_PROMPT)]),
            contents = [GeminiContent(role = turn.role, parts = [GeminiPart(text = turn.text)]) for turn in geminiTurnRequest.history],
            generationConfig = GenerationConfig(
                maxOutputTokens = 1024,      # permite respuestas largas (~2-3 min de voz)
                temperature = 0.7,
                thinkingConfig = ThinkingConfig(thinkingBudget = 0),   # sin "pensar": rapido, cabe en los 8s de Alexa
            ),
        )

        url: str = f"https://generativelanguage.googleapis.com/v1beta/models/{self._settings.geminiModel}:generateContent"

        reply: str = ""
        try:
            # La clave va en cabecera, no en la query: asi no acaba escrita en los
            # logs de acceso ni en el historial de proxies.
            response: httpx.Response = await self._httpClient.post(
                url,
                content = _payloadAdapter.dump_json(payload, exclude_none = True),
                headers = {
                    "Content-Type": "application/json",
                    "x-goog-api-key": self._settings.geminiApiKey,
                },
                timeout = _REQUEST_TIMEOUT_SECONDS,
            )

            if response.status_code != httpx.codes.OK:
                print(f"[Gemini] HTTP {response.status_code}: {response.text}")
                reply = "Lo siento, no he podido pensar la respuesta ahora mismo."
            else:
                reply = self._extractText(response.text)
        except httpx.TimeoutException:
            reply = "Me ha llevado demasiado, puedes repetirlo?"
        except Exception as ex:
            print(f"[Gemini] Error: {ex}")
            reply = "Ha habido un problema al responder."

        geminiTurnRequest.history.append(GeminiTurn(role = "model", text = reply))
        self._trimHistory(geminiTurnRequest.history)

        return GeminiTurnResponse(
            replay = reply,
            updatedHistory = geminiTurnRequest.history,
        )
    # endregion

    # region _trimHistory
    @staticmethod
    def _trimHistory(history: list[GeminiTurn]) -> None:
        """Recorta los turnos mas antiguos hasta caber en el presupuesto.

        Ademas se asegura de que el historial empiece por un turno "user": Gemini
        rechaza una conversacion que arranque con "model".
        """
        total: int = sum(len(turn.text) for turn in history)
        while len(history) > 2 and total > _HISTORY_CHAR_BUDGET:
            total -= len(history[0].text)
            history.pop(0)

        while history and history[0].role == "model":
            history.pop(0)
    # endregion

    # region _extractText
    @staticmethod
    def _extractText(body: str) -> str:
        raiz: dict[str, Any] = json.loads(body)

        candidates: list[dict[str, Any]] = raiz.get("candidates", [])
        if candidates:
            candidate: dict[str, Any] = candidates[0]
            finishReason: str | None = candidate.get("finishReason")

            parts: list[dict[str, Any]] = candidate.get("content", {}).get("parts", [])

            text: str = ""
            for part in parts:
                text += part.get("text", "")

            text = text.strip()

            if len(text) > _MAX_SPEECH_LENGTH:
                text = text[:_MAX_SPEECH_LENGTH]

            if not GeneralUtils.IsNullOrEmpty(text):
                return text

            print(f"[Gemini] sin texto (finishReason={finishReason}): {body}")

        return "No tengo una respuesta para eso."
    # endregion