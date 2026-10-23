using HomeService.Application.DataTransferObject.Roomba.CreateRoomba;
using HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;
using HomeService.Application.Interface.Application;
using HomeService.Domain.Model.Enum.Roomba;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.JsonInterchange.Roomba.CreateRoomba;
using HomeService.Transversal.JsonInterchange.Roomba.PatchRoombaState;
using HomeService.Transversal.Security.Filter;
using HomeService.Transversal.Wrapper.Json;
using Microsoft.AspNetCore.Mvc;

namespace HomeService.Service.WebApi.Controllers
{
    [ApiController]
    [Route("roomba")]
    [ApiKeyAuth]
    public class RoombaController : ControllerBase
    {
        private readonly IRoombaApplication _roombaApplication;

        public RoombaController(IRoombaApplication roombaApplication)
        {
            _roombaApplication = roombaApplication;
        }

        #region CreateRoomba
        [HttpPost("create-roomba")]
        public async Task<IActionResult> CreateRoomba([FromBody] CreateRoombaRequestJson createRoombaRequestJson)
        {
            CreateRoombaResponseJson createRoombaResponseJson = new CreateRoombaResponseJson();

            try
            {
                if (GeneralUtils.IsNullOrEmpty(createRoombaRequestJson.CallOut) ||
                    GeneralUtils.IsNullOrEmpty(createRoombaRequestJson.DeviceName) ||
                    GeneralUtils.IsNullOrEmpty(createRoombaRequestJson.DeviceType))
                {
                    createRoombaResponseJson.ResponseCodeJson = ResponseCodesJson.INVALID_DATA;
                    createRoombaResponseJson.IsSuccess = false;
                    createRoombaResponseJson.Message = "the callout, device name or device type is invalid";
                }
                else
                {
                    CreateRoombaRequest createRoombaRequest = new CreateRoombaRequest
                    {
                        CallOut = createRoombaRequestJson.CallOut,
                        DeviceName = createRoombaRequestJson.DeviceName,
                        DeviceType = createRoombaRequestJson.DeviceType,
                        Model = createRoombaRequestJson.Model,
                        Manufacturer = createRoombaRequestJson.Manufacturer,
                        MacAddress = createRoombaRequestJson.MacAddress,
                    };

                    CreateRoombaResponse createRoombaResponse = await _roombaApplication.CreateRoomba(createRoombaRequest);

                    createRoombaResponseJson.ResponseCodeJson = (ResponseCodesJson)createRoombaResponse.ResponseCode;
                    createRoombaResponseJson.IsSuccess = createRoombaResponse.IsSuccess;
                    createRoombaResponseJson.Message = createRoombaResponse.Message;
                }
            }
            catch (Exception ex)
            {
                createRoombaResponseJson.ResponseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR;
                createRoombaResponseJson.IsSuccess = false;
                createRoombaResponseJson.Message = $"Ha ocurrido un error al crear el roomba {ex.Message}.";
            }

            return ControllerUtils.HttpResults(createRoombaResponseJson.ResponseCodeJson, createRoombaResponseJson);
        }
        #endregion

        #region PatchRoombaState
        [HttpPatch("patch-roomba-state")]
        public async Task<IActionResult> PatchRoombaState([FromBody] PatchRoombaStateRequestJson patchRoombaStateRequestJson)
        {
            PatchRoombaStateResponseJson patchRoombaStateResponseJson = new PatchRoombaStateResponseJson();

            try
            {
                RoombaTarget roombaTarget = GeneralUtils.ParseEnum(patchRoombaStateRequestJson.Target, RoombaTarget.FULL_HOUSE);
                RoombaPhase roombaPhase = GeneralUtils.ParseEnum(patchRoombaStateRequestJson.Phase, RoombaPhase.STOP);

                PatchRoombaStateRequest patchRoombaStateRequest = new PatchRoombaStateRequest
                {
                    IsActivation = patchRoombaStateRequestJson.IsActivation,
                    IsFinished = patchRoombaStateRequestJson.IsFinished,
                    Target = roombaTarget,
                    Phase = roombaPhase,
                    BatteryPercent = patchRoombaStateRequestJson.BatteryPercent,
                    BinFull = patchRoombaStateRequestJson.BinFull,
                    ErrorCode = patchRoombaStateRequestJson.ErrorCode,
                    ErrorMessage = patchRoombaStateRequestJson.ErrorMessage,
                    PmapId = patchRoombaStateRequestJson.PmapId,
                    UserPmapvId = patchRoombaStateRequestJson.UserPmapvId,
                    IsOnline = patchRoombaStateRequestJson.IsOnline,
                };

                PatchRoombaStateResponse patchRoombaStateResponse = await _roombaApplication.PatchRoombaState(patchRoombaStateRequest);

                patchRoombaStateResponseJson.ResponseCodeJson = (ResponseCodesJson)patchRoombaStateResponse.ResponseCode;
                patchRoombaStateResponseJson.IsSuccess = patchRoombaStateResponse.IsSuccess;
                patchRoombaStateResponseJson.Message = patchRoombaStateResponse.Message;
            }
            catch (Exception ex)
            {
                patchRoombaStateResponseJson.ResponseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR;
                patchRoombaStateResponseJson.IsSuccess = false;
                patchRoombaStateResponseJson.Message = $"Ha ocurrido un error al actualizar el estado del roomba {ex.Message}.";
            }

            return ControllerUtils.HttpResults(patchRoombaStateResponseJson.ResponseCodeJson, patchRoombaStateResponseJson);
        }
        #endregion
    }
}
