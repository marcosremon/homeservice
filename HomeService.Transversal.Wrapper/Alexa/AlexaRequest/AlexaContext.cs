using System.Text.Json.Serialization;

namespace HomeService.Transversal.Wrapper.Alexa.AlexaRequest;

public class AlexaContext
{
    // Amazon manda esta clave en mayuscula: "System", no "system".
    [JsonPropertyName("System")]
    public SystemState System { get; set; } = new();
}
