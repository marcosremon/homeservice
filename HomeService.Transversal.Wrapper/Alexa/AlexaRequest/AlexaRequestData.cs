using System.Text.Json.Serialization;

namespace HomeService.Transversal.Wrapper.Alexa.AlexaRequest;

public class AlexaRequestData
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; } = string.Empty;

    [JsonPropertyName("intent")]
    public AlexaIntent? Intent { get; set; }
}
