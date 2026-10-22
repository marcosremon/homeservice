using HomeService.Application.DataTransferObject.Alexa;

namespace HomeService.Application.Interface.Service;

public interface IAlexaService
{
    Task<AlexaResponse> SendAlexaOrder(AlexaRequest alexaRequest);
}
