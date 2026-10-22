using HomeService.Transversal.Wrapper.Alexa.AlexaRequest;

namespace HomeService.Application.DataTransferObject.Alexa;

public class AlexaRequest
{
    public string Version { get; set; } = "1.0";
    public AlexaSession? Session { get; set; }
    public AlexaRequestData AlexaRequestData { get; set; } = new();
    public AlexaContext? Context { get; set; }
}
