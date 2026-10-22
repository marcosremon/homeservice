namespace HomeService.Application.DataTransferObject.Gemini;

public class GeminiTurnRequest
{
    public string UserText { get; set; } = string.Empty;
    public List<GeminiTurn> History { get; set; } = [];
}
