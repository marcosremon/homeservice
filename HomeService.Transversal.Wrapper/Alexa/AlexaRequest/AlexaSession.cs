using System.Text.Json.Serialization;

namespace HomeService.Transversal.Wrapper.Alexa.AlexaRequest;

public class AlexaSession
{
    [JsonPropertyName("new")]
    public bool New { get; set; }

    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [JsonPropertyName("application")]
    public AlexaApplication Application { get; set; } = new AlexaApplication();

    [JsonPropertyName("user")]
    public AlexaUser User { get; set; } = new AlexaUser();

    [JsonPropertyName("attributes")]
    public Dictionary<string, string>? Attributes { get; set; }
}
