using HomeService.Application.DataTransferObject.ChangeComputerStatus.GetComputerStatus;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOffComputer;
using HomeService.Application.DataTransferObject.ChangeComputerStatus.TurnOnComputer;
using HomeService.Application.Interface.Application;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.JsonInterchange.ChangeComputerStatus.GetComputerStatus;
using HomeService.Transversal.JsonInterchange.ChangeComputerStatus.TurnOffComputer;
using HomeService.Transversal.JsonInterchange.ChangeComputerStatus.TurnOnComputer;
using HomeService.Transversal.Security.Filter;
using HomeService.Transversal.Wrapper.Json;
using Microsoft.AspNetCore.Mvc;

namespace HomeService.Service.WebApi.Controllers
{
    [ApiController]
    [Route("change-computer-status")]
    [ApiKeyAuth]
    public class ChangeComputerStatusController : ControllerBase
    {
        private readonly IChangeComputerStatusApplication _changeComputerStatusApplication;

        public ChangeComputerStatusController(IChangeComputerStatusApplication changeComputerStatusApplication)
        {
            _changeComputerStatusApplication = changeComputerStatusApplication;
        }

        #region TurnOnComputer
        [HttpGet("turn-on-computer")]
        public async Task<IActionResult> TurnOnComputer()
        {
            TurnOnComputerResponseJson turnOnComputerResponseJson = new TurnOnComputerResponseJson();

            try
            {
                TurnOnComputerResponse turnOnComputerResponse = await _changeComputerStatusApplication.TurnOnComputer();

                turnOnComputerResponseJson.ResponseCodeJson = (ResponseCodesJson)turnOnComputerResponse.ResponseCode;
                turnOnComputerResponseJson.IsSuccess = turnOnComputerResponse.IsSuccess;
                turnOnComputerResponseJson.Message = turnOnComputerResponse.Message;
            }
            catch (Exception ex)
            {
                turnOnComputerResponseJson.ResponseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR;
                turnOnComputerResponseJson.IsSuccess = false;
                turnOnComputerResponseJson.Message = $"Ha ocurrido un error al encender el ordenador {ex.Message}.";
            }

            return ControllerUtils.HttpResults(turnOnComputerResponseJson.ResponseCodeJson, turnOnComputerResponseJson);
        }
        #endregion

        #region TurnOffComputer
        [HttpGet("turn-off-computer")]
        public async Task<IActionResult> TurnOffComputer()
        {
            TurnOffComputerResponseJson turnOffComputerResponseJson = new TurnOffComputerResponseJson();

            try
            {
                TurnOffComputerResponse turnOffComputerResponse = await _changeComputerStatusApplication.TurnOffComputer();

                turnOffComputerResponseJson.ResponseCodeJson = (ResponseCodesJson)turnOffComputerResponse.ResponseCode;
                turnOffComputerResponseJson.IsSuccess = turnOffComputerResponse.IsSuccess;
                turnOffComputerResponseJson.Message = turnOffComputerResponse.Message;
            }
            catch (Exception ex)
            {
                turnOffComputerResponseJson.ResponseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR;
                turnOffComputerResponseJson.IsSuccess = false;
                turnOffComputerResponseJson.Message = $"Ha ocurrido un error al apagar el ordenador {ex.Message}.";
            }

            return ControllerUtils.HttpResults(turnOffComputerResponseJson.ResponseCodeJson, turnOffComputerResponseJson);
        }
        #endregion

        #region GetComputerStatus
        [HttpGet("get-computer-status")]
        public async Task<IActionResult> GetComputerStatus()
        {
            GetComputerStatusResponseJson getComputerStatusResponseJson = new GetComputerStatusResponseJson();

            try
            {
                GetComputerStatusResponse getComputerStatusResponse = await _changeComputerStatusApplication.GetComputerStatus();

                getComputerStatusResponseJson.ResponseCodeJson = (ResponseCodesJson)getComputerStatusResponse.ResponseCode;
                getComputerStatusResponseJson.IsSuccess = getComputerStatusResponse.IsSuccess;
                getComputerStatusResponseJson.ComputerStatus = getComputerStatusResponse.ComputerStatus;
                getComputerStatusResponseJson.Message = getComputerStatusResponse.Message;
            }
            catch (Exception ex)
            {
                getComputerStatusResponseJson.ResponseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR;
                getComputerStatusResponseJson.IsSuccess = false;
                getComputerStatusResponseJson.ComputerStatus = false;
                getComputerStatusResponseJson.Message = $"Ha ocurrido un error al consultar el estado del ordenador {ex.Message}.";
            }

            return ControllerUtils.HttpResults(getComputerStatusResponseJson.ResponseCodeJson, getComputerStatusResponseJson);
        }
        #endregion
    }
}
