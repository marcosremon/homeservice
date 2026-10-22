namespace HomeService.Application.Interface.Service;

public interface IComputerStatusService
{
    Task<string> ExecuteComputerStatusOrder(string intentName);
}
