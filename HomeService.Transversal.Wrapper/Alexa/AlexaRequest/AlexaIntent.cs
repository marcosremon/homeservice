using System.Text.Json.Serialization;

namespace HomeService.Transversal.Wrapper.Alexa.AlexaRequest;

public class AlexaIntent
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("confirmationStatus")]
    public string ConfirmationStatus { get; set; } = "NONE";

    [JsonPropertyName("slots")]
    public Dictionary<string, AlexaSlot>? Slots { get; set; }
}
