using System.Text.Json.Serialization;
using HomeService.Transversal.Wrapper.Alexa.AlexaRequest;

namespace HomeService.Transversal.JsonInterchange.Alexa;

// Amazon manda "request", no "alexaRequestData": de ahi los JsonPropertyName.
public class AlexaRequestJson
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("session")]
    public AlexaSession? Session { get; set; }

    [JsonPropertyName("request")]
    public AlexaRequestData? AlexaRequestData { get; set; }

    [JsonPropertyName("context")]
    public AlexaContext? Context { get; set; }
}
