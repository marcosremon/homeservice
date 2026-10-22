namespace HomeService.Application.DataTransferObject.Gemini;

public class GeminiTurnResponse
{
    public string Replay { get; set; } = string.Empty;
    public List<GeminiTurn> UpdatedHistory { get; set; } = [];
}
