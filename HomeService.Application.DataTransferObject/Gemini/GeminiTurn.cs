namespace HomeService.Application.DataTransferObject.Gemini;

public class GeminiTurn
{
    // "user" o "model"
    public string Role { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}
