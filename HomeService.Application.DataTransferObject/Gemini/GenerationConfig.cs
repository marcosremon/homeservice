namespace HomeService.Application.DataTransferObject.Gemini;

public class GenerationConfig
{
    public int MaxOutputTokens { get; set; }
    public double Temperature { get; set; }
    public ThinkingConfig? ThinkingConfig { get; set; }
}
