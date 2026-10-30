using System.Text.Json.Serialization;

namespace HomeService.Transversal.Wrapper.Alexa.AlexaResponse;

public class AlexaResponseContent
{
    [JsonPropertyName("outputSpeech")]
    public AlexaOutputSpeech OutputSpeech { get; set; } = new AlexaOutputSpeech();

    [JsonPropertyName("reprompt")]
    public AlexaReprompt? Reprompt { get; set; }

    [JsonPropertyName("shouldEndSession")]
    public bool ShouldEndSession { get; set; } = true;
}
