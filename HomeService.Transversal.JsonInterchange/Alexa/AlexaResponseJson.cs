using System.Text.Json.Serialization;
using HomeService.Transversal.Wrapper.Alexa.AlexaResponse;
using HomeService.Transversal.Wrapper.Json;

namespace HomeService.Transversal.JsonInterchange.Alexa;

public class AlexaResponseJson
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0";

    [JsonPropertyName("sessionAttributes")]
    public Dictionary<string, string>? SessionAttributes { get; set; }

    [JsonPropertyName("response")]
    public AlexaResponseContent AlexaResponseContent { get; set; } = new();

    // Control interno del controlador, no se serializa hacia Amazon.
    [JsonIgnore]
    public BaseResponseJson BaseResponseJson { get; set; } = new();
}
