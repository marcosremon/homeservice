using HomeService.Application.DataTransferObject.ChangeComputerStatus.GetComputerStatus;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOffComputer;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOnComputer;

namespace HomeService.Application.Interface.Application;

public interface IChangeComputerStatusApplication
{
    Task<GetComputerStatusResponse> GetComputerStatus();
    Task<TurnOffComputerResponse> TurnOffComputer();
    Task<TurnOnComputerResponse> TurnOnComputer();
}
