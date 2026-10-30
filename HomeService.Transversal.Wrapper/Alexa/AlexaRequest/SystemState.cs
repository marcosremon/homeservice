using System.Text.Json.Serialization;

namespace HomeService.Transversal.Wrapper.Alexa.AlexaRequest;

public class SystemState
{
    [JsonPropertyName("application")]
    public AlexaApplication Application { get; set; } = new AlexaApplication();

    [JsonPropertyName("user")]
    public AlexaUser User { get; set; } = new AlexaUser();
}
