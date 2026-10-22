namespace HomeService.Application.Interface.Service;

public interface ILightService
{
    Task<string> ExecuteLightOrder(string intentName);
}
