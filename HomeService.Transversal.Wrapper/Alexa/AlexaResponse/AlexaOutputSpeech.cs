using System.Text.Json.Serialization;

namespace HomeService.Transversal.Wrapper.Alexa.AlexaResponse;

public class AlexaOutputSpeech
{
    // "PlainText" o "SSML" para voz
    [JsonPropertyName("type")]
    public string Type { get; set; } = "SSML";

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}
