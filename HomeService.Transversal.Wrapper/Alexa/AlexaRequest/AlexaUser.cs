using System.Text.Json.Serialization;

namespace HomeService.Transversal.Wrapper.Alexa.AlexaRequest;

public class AlexaUser
{
    [JsonPropertyName("userId")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }
}
