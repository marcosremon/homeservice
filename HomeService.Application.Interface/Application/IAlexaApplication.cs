using HomeService.Application.DataTransferObject.Alexa;

namespace HomeService.Application.Interface.Application;

public interface IAlexaApplication
{
    Task<AlexaResponse> SendAlexaOrder(AlexaRequest alexaRequest);
}
