using HomeService.Application.DataTransferObject.ChangeComputerStatus.GetComputerStatus;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOffComputer;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOnComputer;
using HomeService.Application.Interface.Application;
using HomeService.Application.Interface.Repository;

namespace HomeService.Application.UseCase;

public class ChangeComputerStatusApplication : IChangeComputerStatusApplication
{
    private readonly IChangeComputerStatusRepository _changeComputerStatusRepository;

    public ChangeComputerStatusApplication(IChangeComputerStatusRepository changeComputerStatusRepository) =>
        _changeComputerStatusRepository = changeComputerStatusRepository;

    #region GetComputerStatus
    public Task<GetComputerStatusResponse> GetComputerStatus() =>
        _changeComputerStatusRepository.GetComputerStatus();
    #endregion

    #region TurnOffComputer
    public Task<TurnOffComputerResponse> TurnOffComputer() =>
        _changeComputerStatusRepository.TurnOffComputer();
    #endregion

    #region TurnOnComputer
    public Task<TurnOnComputerResponse> TurnOnComputer() =>
        _changeComputerStatusRepository.TurnOnComputer();
    #endregion
}
