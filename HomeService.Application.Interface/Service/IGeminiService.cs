using HomeService.Application.DataTransferObject.Gemini;

namespace HomeService.Application.Interface.Service;

public interface IGeminiService
{
    // Devuelve la respuesta hablada y el historial actualizado para reinyectarlo en
    // sessionAttributes.
    Task<GeminiTurnResponse> Converse(GeminiTurnRequest geminiTurnRequest);
}
