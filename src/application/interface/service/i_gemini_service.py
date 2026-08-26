from abc import ABC, abstractmethod

from application.data_transfer_object.gemini.gemini_turn_request import GeminiTurnRequest
from application.data_transfer_object.gemini.gemini_turn_response import GeminiTurnResponse

class IGeminiService(ABC):
    @abstractmethod
    async def converse(self, gemini_turn_request: GeminiTurnRequest) -> GeminiTurnResponse:
        """Devuelve la respuesta hablada y el historial actualizado para reinyectarlo en sessionAttributes."""
        ...