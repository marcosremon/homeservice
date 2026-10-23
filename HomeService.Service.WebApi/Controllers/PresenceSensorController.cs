using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.CreatePresenceSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.PatchPresenceSensorData;
using HomeService.Application.Interface.Application;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.PresenceSensor.CreatePresenceSensor;
using HomeService.Transversal.JsonInterchange.HomeAutomation.Sensor.PresenceSensor.PatchPresenceSensorData;
using HomeService.Transversal.Security.Filter;
using HomeService.Transversal.Wrapper.Json;
using Microsoft.AspNetCore.Mvc;

namespace HomeService.Service.WebApi.Controllers
{
    // "sensor" en singular, a diferencia de rain y temperature: es la ruta que ya
    // usan los dispositivos, asi que no se toca.
    [ApiController]
    [Route("sensor/presence-sensor")]
    [ApiKeyAuth]
    public class PresenceSensorController : ControllerBase
    {
        private readonly IPresenceSensorApplication _presenceSensorApplication;

        public PresenceSensorController(IPresenceSensorApplication presenceSensorApplication)
        {
            _presenceSensorApplication = presenceSensorApplication;
        }

        #region CreatePresenceSensor
        [HttpPost("create-presence-sensor")]
        public async Task<IActionResult> CreatePresenceSensor([FromBody] CreatePresenceSensorRequestJson createPresenceSensorRequestJson)
        {
            CreatePresenceSensorResponseJson createPresenceSensorResponseJson = new CreatePresenceSensorResponseJson();

            try
            {
                if (GeneralUtils.IsNullOrEmpty(createPresenceSensorRequestJson.CallOut) ||
                    GeneralUtils.IsNullOrEmpty(createPresenceSensorRequestJson.DeviceName) ||
                    GeneralUtils.IsNullOrEmpty(createPresenceSensorRequestJson.DeviceType))
                {
                    createPresenceSensorResponseJson.ResponseCodeJson = ResponseCodesJson.INVALID_DATA;
                    createPresenceSensorResponseJson.IsSuccess = false;
                    createPresenceSensorResponseJson.Message = "the callout, device name or device type is invalid";
                }
                else
                {
                    CreatePresenceSensorRequest createPresenceSensorRequest = new CreatePresenceSensorRequest
                    {
                        CallOut = createPresenceSensorRequestJson.CallOut,
                        DeviceName = createPresenceSensorRequestJson.DeviceName,
                        DeviceType = createPresenceSensorRequestJson.DeviceType,
                        Ts = createPresenceSensorRequestJson.Ts,
                        Presence = createPresenceSensorRequestJson.Presence,
                        DistanceCm = createPresenceSensorRequestJson.DistanceCm,
                        Motion = createPresenceSensorRequestJson.Motion,
                        Model = createPresenceSensorRequestJson.Model,
                        Manufacturer = createPresenceSensorRequestJson.Manufacturer,
                        MacAddress = createPresenceSensorRequestJson.MacAddress,
                    };

                    CreatePresenceSensorResponse createPresenceSensorResponse = await _presenceSensorApplication.CreatePresenceSensor(createPresenceSensorRequest);

                    createPresenceSensorResponseJson.ResponseCodeJson = (ResponseCodesJson)createPresenceSensorResponse.ResponseCode;
                    createPresenceSensorResponseJson.IsSuccess = createPresenceSensorResponse.IsSuccess;
                    createPresenceSensorResponseJson.Message = createPresenceSensorResponse.Message;
                }
            }
            catch (Exception ex)
            {
                createPresenceSensorResponseJson.ResponseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR;
                createPresenceSensorResponseJson.IsSuccess = false;
                createPresenceSensorResponseJson.Message = $"Ha ocurrido un error al crear el sensor de presencia {ex.Message}.";
            }

            return ControllerUtils.HttpResults(createPresenceSensorResponseJson.ResponseCodeJson, createPresenceSensorResponseJson);
        }
        #endregion

        #region PatchPresenceSensorData
        [HttpPatch("patch-presence-sensor-data")]
        public async Task<IActionResult> PatchPresenceSensorData([FromBody] PatchPresenceSensorDataRequestJson patchPresenceSensorDataRequestJson)
        {
            PatchPresenceSensorDataResponseJson patchPresenceSensorDataResponseJson = new PatchPresenceSensorDataResponseJson();

            try
            {
                if (GeneralUtils.IsNullOrEmpty(patchPresenceSensorDataRequestJson.CallOut) ||
                    GeneralUtils.IsNullOrEmpty(patchPresenceSensorDataRequestJson.DeviceName) ||
                    GeneralUtils.IsNullOrEmpty(patchPresenceSensorDataRequestJson.DeviceType))
                {
                    patchPresenceSensorDataResponseJson.ResponseCodeJson = ResponseCodesJson.INVALID_DATA;
                    patchPresenceSensorDataResponseJson.IsSuccess = false;
                    patchPresenceSensorDataResponseJson.Message = "the callout, device name or device type is invalid";
                }
                else
                {
                    PatchPresenceSensorDataRequest patchPresenceSensorDataRequest = new PatchPresenceSensorDataRequest
                    {
                        CallOut = patchPresenceSensorDataRequestJson.CallOut,
                        DeviceName = patchPresenceSensorDataRequestJson.DeviceName,
                        DeviceType = patchPresenceSensorDataRequestJson.DeviceType,
                        Model = patchPresenceSensorDataRequestJson.Model,
                        Manufacturer = patchPresenceSensorDataRequestJson.Manufacturer,
                        MacAddress = patchPresenceSensorDataRequestJson.MacAddress,
                        Ts = patchPresenceSensorDataRequestJson.Ts,
                        Presence = patchPresenceSensorDataRequestJson.Presence,
                        DistanceCm = patchPresenceSensorDataRequestJson.DistanceCm,
                        Motion = patchPresenceSensorDataRequestJson.Motion,
                    };

                    PatchPresenceSensorDataResponse patchPresenceSensorDataResponse = await _presenceSensorApplication.PatchPresenceSensorData(patchPresenceSensorDataRequest);

                    patchPresenceSensorDataResponseJson.ResponseCodeJson = (ResponseCodesJson)patchPresenceSensorDataResponse.ResponseCode;
                    patchPresenceSensorDataResponseJson.IsSuccess = patchPresenceSensorDataResponse.IsSuccess;
                    patchPresenceSensorDataResponseJson.Message = patchPresenceSensorDataResponse.Message;
                }
            }
            catch (Exception ex)
            {
                patchPresenceSensorDataResponseJson.ResponseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR;
                patchPresenceSensorDataResponseJson.IsSuccess = false;
                patchPresenceSensorDataResponseJson.Message = $"Ha ocurrido un error al modificar los datos del sensor de presencia {ex.Message}.";
            }

            return ControllerUtils.HttpResults(patchPresenceSensorDataResponseJson.ResponseCodeJson, patchPresenceSensorDataResponseJson);
        }
        #endregion
    }
}
