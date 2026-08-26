from datetime import datetime, timezone

from fastapi_utils.cbv import cbv
from fastapi import APIRouter, Depends, status
from application.data_transfer_object.roomba.create_roomba.create_roomba_request import CreateRoombaRequest
from application.data_transfer_object.roomba.create_roomba.create_roomba_response import CreateRoombaResponse
from application.data_transfer_object.roomba.patch_roomba_state.patch_roomba_state_request import PatchRoombaStateRequest
from application.data_transfer_object.roomba.patch_roomba_state.patch_roomba_state_response import PatchRoombaStateResponse
from application.interface.application.i_roomba_application import IRoombaApplication
from domain.model.enum.Roomba.roomba_phase import RoombaPhase
from domain.model.enum.Roomba.roomba_target import RoombaTarget
from transversal.security.filter.api_key_auth import get_api_key
from infraestructure.persistence.dependencies.dependency_injection import get_roomba_application
from transversal.common.utils.general_utils import GeneralUtils
from transversal.common.wrappers.json.response_codes_json import ResponseCodesJson
from transversal.json_interchange.roomba.create_roomba.create_roomba_request_json import CreateRoombaRequestJson
from transversal.json_interchange.roomba.create_roomba.create_roomba_response_json import CreateRoombaResponseJson
from transversal.json_interchange.roomba.patch_roomba_state.patch_roomba_state_request_json import PatchRoombaStateRequestJson
from transversal.json_interchange.roomba.patch_roomba_state.patch_roomba_state_response_json import PatchRoombaStateResponseJson

router = APIRouter(
    prefix = "/roomba",
    dependencies = [Depends(get_api_key)],  # equivalente a [ApiKeyAuth] a nivel de clase
)

@cbv(router)
class RoombaController:
    _roomba_application: IRoombaApplication = Depends(get_roomba_application)

    #region CreateRoomba
    @router.post("/create-roomba", response_model = CreateRoombaResponseJson, status_code = status.HTTP_200_OK)
    async def create_roomba(self, create_roomba_request_json: CreateRoombaRequestJson) -> CreateRoombaResponseJson:
        create_roomba_response_json: CreateRoombaResponseJson = CreateRoombaResponseJson()
        try:
            if (GeneralUtils.is_null_or_empty(create_roomba_request_json.callout) or
                GeneralUtils.is_null_or_empty(create_roomba_request_json.device_name) or
                GeneralUtils.is_null_or_empty(create_roomba_request_json.device_type)
            ):
                create_roomba_response_json.response_code_json = ResponseCodesJson.INVALID_DATA
                create_roomba_response_json.is_success = False
                create_roomba_response_json.message = "the callout, device name or device type is invalid"
            else:
                create_roomba_request: CreateRoombaRequest = CreateRoombaRequest(
                    callout = create_roomba_request_json.callout,
                    device_name = create_roomba_request_json.device_name,
                    device_type = create_roomba_request_json.device_type,
                    model = create_roomba_request_json.model,
                    manufacturer = create_roomba_request_json.manufacturer,
                    mac_address = create_roomba_request_json.mac_address,
                )

                create_roomba_response: CreateRoombaResponse = await self._roomba_application.create_roomba(create_roomba_request)

                create_roomba_response_json.response_code_json = ResponseCodesJson(create_roomba_response.response_code)
                create_roomba_response_json.is_success = create_roomba_response.is_success
                create_roomba_response_json.message = create_roomba_response.message
        except Exception as ex:
            create_roomba_response_json.response_code_json = ResponseCodesJson.INTERNAL_SERVER_ERROR
            create_roomba_response_json.is_success = False
            create_roomba_response_json.message = f"Ha ocurrido un error al crear el roomba {ex}."

        return create_roomba_response_json
    #endregion

    #region PatchRoombaState
    @router.post("/patch-roomba-state", response_model = PatchRoombaStateResponseJson, status_code = status.HTTP_200_OK)
    async def patch_roomba_state(self, patch_roomba_state_request_json: PatchRoombaStateRequestJson) -> PatchRoombaStateResponseJson:
        patch_roomba_state_response_json: PatchRoombaStateResponseJson = PatchRoombaStateResponseJson()
        try:
            # Equivalente a Enum.TryParse(..., ignoreCase: true, out ...): si el
            # nombre no existe se cae al valor por defecto en vez de reventar.
            roomba_target: RoombaTarget = GeneralUtils.parse_enum(RoombaTarget, patch_roomba_state_request_json.target, RoombaTarget.FULL_HOUSE)
            roomba_phase: RoombaPhase = GeneralUtils.parse_enum(RoombaPhase, patch_roomba_state_request_json.phase, RoombaPhase.STOP)

            # Equivalente al `== default` de C#: si no mandan hora, se pone la de ahora.
            event_time: datetime = (
                datetime.now(timezone.utc)
                if patch_roomba_state_request_json.event_time == datetime.min
                else patch_roomba_state_request_json.event_time
            )

            patch_roomba_state_request: PatchRoombaStateRequest = PatchRoombaStateRequest(
                event_time = event_time,
                is_activation = patch_roomba_state_request_json.is_activation,
                is_finished = patch_roomba_state_request_json.is_finished,
                target = roomba_target,
                phase = roomba_phase,
                battery_percent = patch_roomba_state_request_json.battery_percent,
                bin_full = patch_roomba_state_request_json.bin_full,
                error_code = patch_roomba_state_request_json.error_code,
                error_message = patch_roomba_state_request_json.error_message,
                pmap_id = patch_roomba_state_request_json.pmap_id,
                user_pmapv_id = patch_roomba_state_request_json.user_pmapv_id,
                is_online = patch_roomba_state_request_json.is_online,
            )

            patch_roomba_state_response: PatchRoombaStateResponse = await self._roomba_application.patch_roomba_state(patch_roomba_state_request)

            patch_roomba_state_response_json.response_code_json = ResponseCodesJson(patch_roomba_state_response.response_code)
            patch_roomba_state_response_json.is_success = patch_roomba_state_response.is_success
            patch_roomba_state_response_json.message = patch_roomba_state_response.message
        except Exception as ex:
            patch_roomba_state_response_json.response_code_json = ResponseCodesJson.INTERNAL_SERVER_ERROR
            patch_roomba_state_response_json.is_success = False
            patch_roomba_state_response_json.message = f"Ha ocurrido un error al actualizar el estado del roomba {ex}."

        return patch_roomba_state_response_json
    #endregion
