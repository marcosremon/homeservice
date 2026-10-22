using HomeService.Application.DataTransferObject.Alexa;

namespace HomeService.Application.Interface.Service;

public interface ITemperatureSensorService
{
    Task<string> ExecuteTemperatureSensorOrder(string intentName, AlexaRequest alexaRequest);
}
