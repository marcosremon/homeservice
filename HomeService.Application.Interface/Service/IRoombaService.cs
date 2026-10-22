using HomeService.Application.DataTransferObject.Alexa;

namespace HomeService.Application.Interface.Service;

public interface IRoombaService
{
    Task<string> ExecuteRoombaOrder(string intentName, AlexaRequest alexaRequest);
}
