using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.CreateTemperatureSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.PatchTemperatureSensor;
using HomeService.Application.Interface.Application;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.TemperatureSensor.CreateTemperatureSensor;
using HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.TemperatureSensor.PatchTemperatureSensor;
using HomeService.Transversal.Security.Filter;
using HomeService.Transversal.Wrapper.Json;
using Microsoft.AspNetCore.Mvc;

namespace HomeService.Service.WebApi.Controllers
{
    [ApiController]
    [Route("sensors/temperature-sensor")]
    [ApiKeyAuth]
    public class TemperatureSensorController : ControllerBase
    {
        private readonly ITemperatureSensorApplication _temperatureSensorApplication;

        public TemperatureSensorController(ITemperatureSensorApplication temperatureSensorApplication)
        {
            _temperatureSensorApplication = temperatureSensorApplication;
        }

        #region CreateTemperatureSensor
        [HttpPost("create-temperature-sensor")]
        public async Task<IActionResult> CreateTemperatureSensor([FromBody] CreateTemperatureSensorRequestJson createTemperatureSensorRequestJson)
        {
            CreateTemperatureSensorResponseJson createTemperatureSensorResponseJson = new CreateTemperatureSensorResponseJson();

            try
            {
                if (GeneralUtils.IsNullOrEmpty(createTemperatureSensorRequestJson.CallOut) ||
                    GeneralUtils.IsNullOrEmpty(createTemperatureSensorRequestJson.DeviceName) ||
                    GeneralUtils.IsNullOrEmpty(createTemperatureSensorRequestJson.DeviceType) ||
                    GeneralUtils.IsNullOrEmpty(createTemperatureSensorRequestJson.Model) ||
                    GeneralUtils.IsNullOrEmpty(createTemperatureSensorRequestJson.Manufacturer) ||
                    createTemperatureSensorRequestJson.Temperature == 0.0 ||
                    createTemperatureSensorRequestJson.AdcVoltage == 0.0 ||
                    createTemperatureSensorRequestJson.MeasureAt == 0.0)
                {
                    createTemperatureSensorResponseJson.ResponseCodeJson = ResponseCodesJson.INVALID_DATA;
                    createTemperatureSensorResponseJson.IsSuccess = false;
                    createTemperatureSensorResponseJson.Message = "the data is invalid";
                }
                else
                {
                    CreateTemperatureSensorRequest createTemperatureSensorRequest = new CreateTemperatureSensorRequest
                    {
                        CallOut = createTemperatureSensorRequestJson.CallOut,
                        DeviceName = createTemperatureSensorRequestJson.DeviceName,
                        DeviceType = createTemperatureSensorRequestJson.DeviceType,
                        Model = createTemperatureSensorRequestJson.Model,
                        Manufacturer = createTemperatureSensorRequestJson.Manufacturer,
                        MacAddress = createTemperatureSensorRequestJson.MacAddress,
                        Temperature = createTemperatureSensorRequestJson.Temperature,
                        AdcVoltage = createTemperatureSensorRequestJson.AdcVoltage,
                        MeasureAt = createTemperatureSensorRequestJson.MeasureAt,
                    };

                    CreateTemperatureSensorResponse createTemperatureSensorResponse = await _temperatureSensorApplication.CreateTemperatureSensor(createTemperatureSensorRequest);

                    createTemperatureSensorResponseJson.ResponseCodeJson = (ResponseCodesJson)createTemperatureSensorResponse.ResponseCode;
                    createTemperatureSensorResponseJson.IsSuccess = createTemperatureSensorResponse.IsSuccess;
                    createTemperatureSensorResponseJson.Message = createTemperatureSensorResponse.Message;
                }
            }
            catch (Exception ex)
            {
                createTemperatureSensorResponseJson.ResponseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR;
                createTemperatureSensorResponseJson.IsSuccess = false;
                createTemperatureSensorResponseJson.Message = $"Ha ocurrido un error al crear el sensor de temperatura {ex.Message}.";
            }

            return ControllerUtils.HttpResults(createTemperatureSensorResponseJson.ResponseCodeJson, createTemperatureSensorResponseJson);
        }
        #endregion

        #region PatchTemperatureSensor
        [HttpPatch("patch-temperature-sensor")]
        public async Task<IActionResult> PatchTemperatureSensor([FromBody] PatchTemperatureSensorRequestJson patchTemperatureSensorRequestJson)
        {
            PatchTemperatureSensorResponseJson patchTemperatureSensorResponseJson = new PatchTemperatureSensorResponseJson();

            try
            {
                if (GeneralUtils.IsNullOrEmpty(patchTemperatureSensorRequestJson.CallOut) ||
                    GeneralUtils.IsNullOrEmpty(patchTemperatureSensorRequestJson.DeviceName) ||
                    GeneralUtils.IsNullOrEmpty(patchTemperatureSensorRequestJson.DeviceType))
                {
                    patchTemperatureSensorResponseJson.ResponseCodeJson = ResponseCodesJson.INVALID_DATA;
                    patchTemperatureSensorResponseJson.IsSuccess = false;
                    patchTemperatureSensorResponseJson.Message = "the callOut, device name or device type is invalid";
                }
                else
                {
                    PatchTemperatureSensorRequest patchTemperatureSensorRequest = new PatchTemperatureSensorRequest
                    {
                        CallOut = patchTemperatureSensorRequestJson.CallOut,
                        DeviceName = patchTemperatureSensorRequestJson.DeviceName,
                        DeviceType = patchTemperatureSensorRequestJson.DeviceType,
                        Model = patchTemperatureSensorRequestJson.Model,
                        MacAddress = patchTemperatureSensorRequestJson.MacAddress,
                        Temperature = patchTemperatureSensorRequestJson.Temperature,
                        AdcVoltage = patchTemperatureSensorRequestJson.AdcVoltage,
                        MeasureAt = patchTemperatureSensorRequestJson.MeasureAt,
                    };

                    PatchTemperatureSensorResponse patchTemperatureSensorResponse = await _temperatureSensorApplication.PatchTemperatureSensor(patchTemperatureSensorRequest);

                    patchTemperatureSensorResponseJson.ResponseCodeJson = (ResponseCodesJson)patchTemperatureSensorResponse.ResponseCode;
                    patchTemperatureSensorResponseJson.IsSuccess = patchTemperatureSensorResponse.IsSuccess;
                    patchTemperatureSensorResponseJson.Message = patchTemperatureSensorResponse.Message;
                }
            }
            catch (Exception ex)
            {
                patchTemperatureSensorResponseJson.ResponseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR;
                patchTemperatureSensorResponseJson.IsSuccess = false;
                patchTemperatureSensorResponseJson.Message = $"Ha ocurrido un error al modificar los datos del sensor de temperatura {ex.Message}.";
            }

            return ControllerUtils.HttpResults(patchTemperatureSensorResponseJson.ResponseCodeJson, patchTemperatureSensorResponseJson);
        }
        #endregion
    }
}
