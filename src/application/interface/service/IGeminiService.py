from abc import ABC, abstractmethod

from application.data_transfer_object.gemini.GeminiTurnRequest import GeminiTurnRequest
from application.data_transfer_object.gemini.GeminiTurnResponse import GeminiTurnResponse

class IGeminiService(ABC):
    @abstractmethod
    async def converse(self, geminiTurnRequest: GeminiTurnRequest) -> GeminiTurnResponse:
        """Devuelve la respuesta hablada y el historial actualizado para reinyectarlo en sessionAttributes."""
        ...