namespace HomeService.Application.DataTransferObject.Gemini;

public class GeminiPayload
{
    public SystemInstruction SystemInstruction { get; set; } = new SystemInstruction();
    public List<GeminiContent> Contents { get; set; } = [];
    public GenerationConfig GenerationConfig { get; set; } = new GenerationConfig();
}
