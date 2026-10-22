using HomeService.Application.DataTransferObject.Alexa;
using HomeService.Application.Interface.Application;
using HomeService.Application.Interface.Service;

namespace HomeService.Application.UseCase;

public class AlexaApplication : IAlexaApplication
{
    private readonly IAlexaService _alexaService;

    public AlexaApplication(IAlexaService alexaService) => _alexaService = alexaService;

    #region SendAlexaOrder
    public Task<AlexaResponse> SendAlexaOrder(AlexaRequest alexaRequest) =>
        _alexaService.SendAlexaOrder(alexaRequest);
    #endregion
}
