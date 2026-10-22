namespace HomeService.Application.DataTransferObject.Gemini;

public class GeminiContent
{
    public string Role { get; set; } = string.Empty;
    public List<GeminiPart> Parts { get; set; } = [];
}
