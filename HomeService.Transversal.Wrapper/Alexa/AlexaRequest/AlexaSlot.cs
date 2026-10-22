using System.Text.Json.Serialization;

namespace HomeService.Transversal.Wrapper.Alexa.AlexaRequest;

public class AlexaSlot
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}
