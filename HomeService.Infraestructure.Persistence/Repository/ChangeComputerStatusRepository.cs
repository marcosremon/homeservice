using HomeService.Application.DataTransferObject.ChangeComputerStatus.GetComputerStatus;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOffComputer;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOnComputer;
using HomeService.Application.Interface.Repository;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Wrapper.Base;

namespace HomeService.Infraestructure.Persistence.Repository
{
    public class ChangeComputerStatusRepository : IChangeComputerStatusRepository
    {
        private readonly Settings _settings;

        public ChangeComputerStatusRepository(Settings settings)
        {
            _settings = settings;
        }

        #region TurnOnComputer
        public async Task<TurnOnComputerResponse> TurnOnComputer()
        {
            TurnOnComputerResponse turnOnComputerResponse = new TurnOnComputerResponse();

            try
            {
            }
            catch (Exception ex)
            {
                turnOnComputerResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                turnOnComputerResponse.IsSuccess = false;
                turnOnComputerResponse.Message = $"Unexpected error on ChangeComputerStatusRepository -> TurnOnComputer: {ex.Message}";
            }

            return turnOnComputerResponse;
        }
        #endregion

        #region TurnOffComputer
        public async Task<TurnOffComputerResponse> TurnOffComputer()
        {
            TurnOffComputerResponse turnOffComputerResponse = new TurnOffComputerResponse();

            try
            {
            }
            catch (Exception ex)
            {
                turnOffComputerResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                turnOffComputerResponse.IsSuccess = false;
                turnOffComputerResponse.Message = $"Unexpected error on ChangeComputerStatusRepository -> TurnOffComputer: {ex.Message}";
            }

            return turnOffComputerResponse;
        }
        #endregion

        #region GetComputerStatus
        public async Task<GetComputerStatusResponse> GetComputerStatus()
        {
            GetComputerStatusResponse getComputerStatusResponse = new GetComputerStatusResponse();

            try
            {
            }
            catch (Exception ex)
            {
                getComputerStatusResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                getComputerStatusResponse.ComputerStatus = false;
                getComputerStatusResponse.IsSuccess = false;
                getComputerStatusResponse.Message = $"Unexpected error on ChangeComputerStatusRepository -> GetComputerStatus: {ex.Message}";
            }

            return getComputerStatusResponse;
        }
        #endregion
    }
}
