using HomeService.Application.DataTransferObject.ChangeComputerStatus.GetComputerStatus;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOffComputer;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOnComputer;
using HomeService.Application.Interface.Repository;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.Wrapper.Base;

namespace HomeService.Infraestructure.Persistence.Repository
{
    // No toca la base de datos: habla con el ordenador por la LAN (ping, Wake on LAN
    // y SSH) a traves de ComputerStatusUtils.
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
                string macAddress = _settings.LanComputerMac;
                string broadcastIp = _settings.LanBroadcastIp;
                string ipAddress = _settings.LanComputerIp;

                if (GeneralUtils.IsNullOrEmpty(macAddress) ||
                    GeneralUtils.IsNullOrEmpty(broadcastIp) ||
                    GeneralUtils.IsNullOrEmpty(ipAddress))
                {
                    turnOnComputerResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                    turnOnComputerResponse.IsSuccess = false;
                    turnOnComputerResponse.Message = "Missing LanParameters in settings (LAN_COMPUTER_MAC, LAN_BROADCAST_IP, LAN_COMPUTER_IP)";
                }
                else if (await ComputerStatusUtils.ComputerStatus(ipAddress))
                {
                    turnOnComputerResponse.ResponseCode = ResponseCodes.OK;
                    turnOnComputerResponse.IsSuccess = true;
                    turnOnComputerResponse.Message = "PC already on. Nothing to do.";
                }
                else
                {
                    await ComputerStatusUtils.SendWakeOnLanPacket(macAddress, broadcastIp);

                    turnOnComputerResponse.ResponseCode = ResponseCodes.OK;
                    turnOnComputerResponse.IsSuccess = true;
                    turnOnComputerResponse.Message = "WoL sent. PC booting into CachyOS.";
                }
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
                string ipAddress = _settings.LanComputerIp;
                string cachyOsUser = _settings.LanCachyosUser;

                if (GeneralUtils.IsNullOrEmpty(ipAddress) || GeneralUtils.IsNullOrEmpty(cachyOsUser))
                {
                    turnOffComputerResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                    turnOffComputerResponse.IsSuccess = false;
                    turnOffComputerResponse.Message = "LanParameters incompletos en settings (LAN_COMPUTER_IP, LAN_CACHYOS_USER)";
                }
                else
                {
                    // Si ya esta apagado no lanza el ssh y devuelve un aviso; si el ssh
                    // falla, ShutdownComputer lanza y lo recoge el catch.
                    string message = await ComputerStatusUtils.ShutdownComputer(ipAddress, cachyOsUser);

                    turnOffComputerResponse.ResponseCode = ResponseCodes.OK;
                    turnOffComputerResponse.IsSuccess = true;
                    turnOffComputerResponse.Message = message;
                }
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
                string ipAddress = _settings.LanComputerIp;

                if (GeneralUtils.IsNullOrEmpty(ipAddress))
                {
                    getComputerStatusResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                    getComputerStatusResponse.IsSuccess = false;
                    getComputerStatusResponse.Message = "The ip address is empty on settings (LAN_COMPUTER_IP)";
                }
                else
                {
                    bool computerStatus = await ComputerStatusUtils.ComputerStatus(ipAddress);

                    getComputerStatusResponse.ResponseCode = ResponseCodes.OK;
                    getComputerStatusResponse.ComputerStatus = computerStatus;
                    getComputerStatusResponse.IsSuccess = true;
                    getComputerStatusResponse.Message = computerStatus ? "ON" : "OFF";
                }
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
