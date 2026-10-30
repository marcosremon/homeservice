using HomeService.Transversal.Wrapper.Alexa.AlexaResponse;

namespace HomeService.Application.DataTransferObject.Alexa;

public class AlexaResponse
{
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, string>? SessionAttributes { get; set; }
    public AlexaResponseContent AlexaResponseContent { get; set; } = new AlexaResponseContent();
}
