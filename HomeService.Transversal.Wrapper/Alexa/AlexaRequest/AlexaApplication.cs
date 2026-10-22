using System.Text.Json.Serialization;

namespace HomeService.Transversal.Wrapper.Alexa.AlexaRequest;

public class AlexaApplication
{
    [JsonPropertyName("applicationId")]
    public string ApplicationId { get; set; } = string.Empty;
}
