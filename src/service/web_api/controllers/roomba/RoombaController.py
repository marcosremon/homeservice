from datetime import datetime, timezone

from fastapi_utils.cbv import cbv
from fastapi import APIRouter, Depends, status
from application.data_transfer_object.roomba.create_roomba.CreateRoombaRequest import CreateRoombaRequest
from application.data_transfer_object.roomba.create_roomba.CreateRoombaResponse import CreateRoombaResponse
from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateRequest import PatchRoombaStateRequest
from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateResponse import PatchRoombaStateResponse
from application.interface.application.IRoombaApplication import IRoombaApplication
from domain.model.enum.Roomba.RoombaPhase import RoombaPhase
from domain.model.enum.Roomba.RoombaTarget import RoombaTarget
from transversal.security.filter.ApiKeyAuth import GetApiKey
from infraestructure.persistence.dependencies.DependencyInjection import GetRoombaApplication
from transversal.common.utils.GeneralUtils import GeneralUtils
from transversal.common.wrappers.json.ResponseCodesJson import ResponseCodesJson
from transversal.json_interchange.roomba.create_roomba.CreateRoombaRequestJson import CreateRoombaRequestJson
from transversal.json_interchange.roomba.create_roomba.CreateRoombaResponseJson import CreateRoombaResponseJson
from transversal.json_interchange.roomba.patch_roomba_state.PatchRoombaStateRequestJson import PatchRoombaStateRequestJson
from transversal.json_interchange.roomba.patch_roomba_state.PatchRoombaStateResponseJson import PatchRoombaStateResponseJson

router = APIRouter(
    prefix = "/roomba",
    dependencies = [Depends(GetApiKey)],  # equivalente a [ApiKeyAuth] a nivel de clase
)

@cbv(router)
class RoombaController:
    _roombaApplication: IRoombaApplication = Depends(GetRoombaApplication)

    #region CreateRoomba
    @router.post("/create-roomba", response_model = CreateRoombaResponseJson, status_code = status.HTTP_200_OK)
    async def CreateRoomba(self, createRoombaRequestJson: CreateRoombaRequestJson) -> CreateRoombaResponseJson:
        createRoombaResponseJson: CreateRoombaResponseJson = CreateRoombaResponseJson()
        try:
            if (GeneralUtils.IsNullOrEmpty(createRoombaRequestJson.callout) or
                GeneralUtils.IsNullOrEmpty(createRoombaRequestJson.deviceName) or
                GeneralUtils.IsNullOrEmpty(createRoombaRequestJson.deviceType)
            ):
                createRoombaResponseJson.responseCodeJson = ResponseCodesJson.INVALID_DATA
                createRoombaResponseJson.isSuccess = False
                createRoombaResponseJson.message = "the callout, device name or device type is invalid"
            else:
                createRoombaRequest: CreateRoombaRequest = CreateRoombaRequest(
                    callout = createRoombaRequestJson.callout,
                    deviceName = createRoombaRequestJson.deviceName,
                    deviceType = createRoombaRequestJson.deviceType,
                    model = createRoombaRequestJson.model,
                    manufacturer = createRoombaRequestJson.manufacturer,
                    macAddress = createRoombaRequestJson.macAddress,
                )

                createRoombaResponse: CreateRoombaResponse = await self._roombaApplication.CreateRoomba(createRoombaRequest)

                createRoombaResponseJson.responseCodeJson = ResponseCodesJson(createRoombaResponse.responseCode)
                createRoombaResponseJson.isSuccess = createRoombaResponse.isSuccess
                createRoombaResponseJson.message = createRoombaResponse.message
        except Exception as ex:
            createRoombaResponseJson.responseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR
            createRoombaResponseJson.isSuccess = False
            createRoombaResponseJson.message = f"Ha ocurrido un error al crear el roomba {ex}."

        return createRoombaResponseJson
    #endregion

    #region PatchRoombaState
    @router.post("/patch-roomba-state", response_model = PatchRoombaStateResponseJson, status_code = status.HTTP_200_OK)
    async def PatchRoombaState(self, patchRoombaStateRequestJson: PatchRoombaStateRequestJson) -> PatchRoombaStateResponseJson:
        patchRoombaStateResponseJson: PatchRoombaStateResponseJson = PatchRoombaStateResponseJson()
        try:
            roombaTarget: RoombaTarget = GeneralUtils.ParseEnum(RoombaTarget, patchRoombaStateRequestJson.target, RoombaTarget.FULL_HOUSE)
            roombaPhase: RoombaPhase = GeneralUtils.ParseEnum(RoombaPhase, patchRoombaStateRequestJson.phase, RoombaPhase.STOP)

            eventTime: datetime = (
                datetime.now(timezone.utc)
                if patchRoombaStateRequestJson.eventTime == datetime.min
                else patchRoombaStateRequestJson.eventTime
            )

            patchRoombaStateRequest: PatchRoombaStateRequest = PatchRoombaStateRequest(
                eventTime = eventTime,
                isActivation = patchRoombaStateRequestJson.isActivation,
                isFinished = patchRoombaStateRequestJson.isFinished,
                target = roombaTarget,
                phase = roombaPhase,
                batteryPercent = patchRoombaStateRequestJson.batteryPercent,
                binFull = patchRoombaStateRequestJson.binFull,
                errorCode = patchRoombaStateRequestJson.errorCode,
                errorMessage = patchRoombaStateRequestJson.errorMessage,
                pmapId = patchRoombaStateRequestJson.pmapId,
                userPmapvId = patchRoombaStateRequestJson.userPmapvId,
                isOnline = patchRoombaStateRequestJson.isOnline,
            )

            patchRoombaStateResponse: PatchRoombaStateResponse = await self._roombaApplication.PatchRoombaState(patchRoombaStateRequest)

            patchRoombaStateResponseJson.responseCodeJson = ResponseCodesJson(patchRoombaStateResponse.responseCode)
            patchRoombaStateResponseJson.isSuccess = patchRoombaStateResponse.isSuccess
            patchRoombaStateResponseJson.message = patchRoombaStateResponse.message
        except Exception as ex:
            patchRoombaStateResponseJson.responseCodeJson = ResponseCodesJson.INTERNAL_SERVER_ERROR
            patchRoombaStateResponseJson.isSuccess = False
            patchRoombaStateResponseJson.message = f"Ha ocurrido un error al actualizar el estado del roomba {ex}."

        return patchRoombaStateResponseJson
    #endregion