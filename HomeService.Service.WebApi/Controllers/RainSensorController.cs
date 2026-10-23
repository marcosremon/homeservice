using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.CreateRainSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.PatchRainSensor;
using HomeService.Application.Interface.Application;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.RainSensor.CreateRainSensor;
using HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.RainSensor.PatchRainSensor;
using HomeService.Transversal.Security.Filter;
using HomeService.Transversal.Wrapper.Json;
using Microsoft.AspNetCore.Mvc;

namespace HomeService.Service.WebApi.Controllers
{
    [ApiController]
    [Route("sensors/rain-sensor")]
    [ApiKeyAuth]
    public class RainSensorController : ControllerBase
    {
        private readonly IRainSensorApplication _rainSensorApplication;

        public RainSensorController(IRainSensorApplication rainSensorApplication)
        {
            _rainSensorApplication = rainSensorApplication;
        }

        #region CreateRainSensor
        [HttpPost("create-rain-sensor")]
        public async Task<IActionResult> CreateRainSensor([FromBody] CreateRainSensorRequestJson createRainSensorRequestJson)
        {
            CreateRainSensorResponseJson createRainSensorResponseJson = new CreateRainSensorResponseJson();

            try
            {
                if (GeneralUtils.IsNullOrEmpty(createRainSensorRequestJson.CallOut) ||
                    GeneralUtils.IsNullOrEmpty(createRainSensorRequestJson.DeviceName) ||
                    GeneralUtils.IsNullOrEmpty(createRainSensorRequestJson.DeviceType) ||
                    GeneralUtils.IsNullOrEmpty(createRainSensorRequestJson.Model) ||
                    GeneralUtils.IsNullOrEmpty(createRainSensorRequestJson.Manufacturer))
                {
                    createRainSensorResponseJson.ResponseCodeJson = ResponseCodesJson.INVALID_DATA;
                    createRainSensorResponseJson.IsSuccess = false;
                    createRainSensorResponseJson.Message = "the data is invalid";
                }
                else
                {
                    CreateRainSensorRequest createRainSensorRequest = new CreateRainSensorRequest
                    {
                        CallOut = createRainSensorRequestJson.CallOut,
                        DeviceName = createRainSensorRequestJson.DeviceName,
                        DeviceType = createRainSensorRequestJson.DeviceType,
                        Model = createRainSensorRequestJson.Model,
                        Manufacturer = createRainSensorRequestJson.Manufacturer,
                        MacAddress = createRainSensorRequestJson.MacAddress,
                        AdcValue = createRainSensorRequestJson.AdcValue,
                        WetnessPercent = createRainSensorRequestJson.WetnessPercent,
                        IsRaining = createRainSensorRequestJson.IsRaining,
                    };

                    CreateRainSensorResponse createRainSensorResponse = await _rainSensorApplication.CreateRainSensor(createRainSensorRequest);

                    createRainSensorResponseJson.ResponseCodeJson = (ResponseCodesJson)createRainSensorResponse.ResponseCode;
                    createRainSensorResponseJson.IsSuccess = createRainSensorResponse.IsSuccess;
                    createRainSensorResponseJson.Message = createRainSensorResponse.Message;
                }
            }
            catch (Exception ex)
            {
                createRainSensorResponseJson.ResponseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR;
                createRainSensorResponseJson.IsSuccess = false;
                createRainSensorResponseJson.Message = $"Ha ocurrido un error al crear el sensor de lluvia {ex.Message}.";
            }

            return ControllerUtils.HttpResults(createRainSensorResponseJson.ResponseCodeJson, createRainSensorResponseJson);
        }
        #endregion

        #region PatchRainSensor
        [HttpPatch("patch-rain-sensor")]
        public async Task<IActionResult> PatchRainSensor([FromBody] PatchRainSensorRequestJson patchRainSensorRequestJson)
        {
            PatchRainSensorResponseJson patchRainSensorResponseJson = new PatchRainSensorResponseJson();

            try
            {
                if (GeneralUtils.IsNullOrEmpty(patchRainSensorRequestJson.CallOut) ||
                    GeneralUtils.IsNullOrEmpty(patchRainSensorRequestJson.DeviceName) ||
                    GeneralUtils.IsNullOrEmpty(patchRainSensorRequestJson.DeviceType))
                {
                    patchRainSensorResponseJson.ResponseCodeJson = ResponseCodesJson.INVALID_DATA;
                    patchRainSensorResponseJson.IsSuccess = false;
                    patchRainSensorResponseJson.Message = "the callOut, device name or device type is invalid";
                }
                else
                {
                    PatchRainSensorRequest patchRainSensorRequest = new PatchRainSensorRequest
                    {
                        CallOut = patchRainSensorRequestJson.CallOut,
                        DeviceName = patchRainSensorRequestJson.DeviceName,
                        DeviceType = patchRainSensorRequestJson.DeviceType,
                        Model = patchRainSensorRequestJson.Model,
                        MacAddress = patchRainSensorRequestJson.MacAddress,
                        AdcValue = patchRainSensorRequestJson.AdcValue,
                        WetnessPercent = patchRainSensorRequestJson.WetnessPercent,
                        IsRaining = patchRainSensorRequestJson.IsRaining,
                    };

                    PatchRainSensorResponse patchRainSensorResponse = await _rainSensorApplication.PatchRainSensor(patchRainSensorRequest);

                    patchRainSensorResponseJson.ResponseCodeJson = (ResponseCodesJson)patchRainSensorResponse.ResponseCode;
                    patchRainSensorResponseJson.IsSuccess = patchRainSensorResponse.IsSuccess;
                    patchRainSensorResponseJson.Message = patchRainSensorResponse.Message;
                    patchRainSensorResponseJson.RainStarted = patchRainSensorResponse.RainStarted;
                }
            }
            catch (Exception ex)
            {
                patchRainSensorResponseJson.ResponseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR;
                patchRainSensorResponseJson.IsSuccess = false;
                patchRainSensorResponseJson.Message = $"Ha ocurrido un error al modificar los datos del sensor de lluvia {ex.Message}.";
            }

            return ControllerUtils.HttpResults(patchRainSensorResponseJson.ResponseCodeJson, patchRainSensorResponseJson);
        }
        #endregion
    }
}
