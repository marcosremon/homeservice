using System.Text.Json.Serialization;

namespace HomeService.Transversal.Wrapper.Alexa.AlexaResponse;

public class AlexaReprompt
{
    [JsonPropertyName("outputSpeech")]
    public AlexaOutputSpeech OutputSpeech { get; set; } = new();
}
