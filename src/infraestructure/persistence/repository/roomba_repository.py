from datetime import datetime, timezone

from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession

from application.data_transfer_object.roomba.create_roomba.create_roomba_request import CreateRoombaRequest
from application.data_transfer_object.roomba.create_roomba.create_roomba_response import CreateRoombaResponse
from application.data_transfer_object.roomba.patch_roomba_state.patch_roomba_state_request import PatchRoombaStateRequest
from application.data_transfer_object.roomba.patch_roomba_state.patch_roomba_state_response import PatchRoombaStateResponse
from application.interface.repository.i_roomba_repository import IRoombaRepository
from domain.model.entity.device import Device
from domain.model.entity.house_zone import HouseZone
from domain.model.entity.roomba import Roomba
from transversal.common.utils.general_utils import GenericUtils
from transversal.common.wrappers.base.response_codes import ResponseCodes

class RoombaRepository(IRoombaRepository):

    def __init__(self, session: AsyncSession):
        self._session = session

    #region create_roomba
    async def create_roomba(self, create_roomba_request: CreateRoombaRequest) -> CreateRoombaResponse:
        create_roomba_response: CreateRoombaResponse = CreateRoombaResponse()
        try:
            async with self._session.begin():
                any_roomba: Roomba | None = await self._session.scalar(select(Roomba).limit(1))
                if any_roomba is not None:
                    create_roomba_response.response_code = ResponseCodes.ANY_ROOMBA_EXIST
                    create_roomba_response.is_success = False
                    create_roomba_response.message = "Any roomba exists on bbdd"
                else:
                    house_zone: HouseZone | None = await self._session.scalar(select(HouseZone)
                                                                             .where(HouseZone.callout == create_roomba_request.callout))
                    if house_zone is None:
                        house_zone = HouseZone(
                            callout = create_roomba_request.callout
                        )
                        self._session.add(house_zone)
                        await self._session.flush()

                    device: Device | None = await self._session.scalar(select(Device).where(
                        Device.house_zone_id == house_zone.house_zone_id,
                        Device.device_name == create_roomba_request.device_name,
                        Device.device_type == create_roomba_request.device_type
                    ))
                    if device is None:
                        device = Device(
                            house_zone_id = house_zone.house_zone_id,
                            device_name = create_roomba_request.device_name,
                            device_type = create_roomba_request.device_type,
                            model = create_roomba_request.model,
                            manufacturer = create_roomba_request.manufacturer,
                            mac_address = create_roomba_request.mac_address,
                        )
                        self._session.add(device)
                        await self._session.flush()

                    roomba: Roomba = Roomba(
                        device_id = device.device_id,
                        last_roomba_activation = datetime.min,
                        last_target = "",
                        last_roomba_end = datetime.min,
                        last_seen = datetime.min,
                    )
                    self._session.add(roomba)

                    create_roomba_response.response_code = ResponseCodes.CREATED
                    create_roomba_response.is_success = True
                    create_roomba_response.message = "roomba created"
        except Exception as ex:
            create_roomba_response.response_code = ResponseCodes.UNEXPECTED_ERROR
            create_roomba_response.is_success = False
            create_roomba_response.message = f"Unexpected error on RoombaRepository -> create_roomba: {ex}"

        return create_roomba_response
    #endregion

    #region patch_roomba_state
    async def patch_roomba_state(self, patch_roomba_state_request: PatchRoombaStateRequest) -> PatchRoombaStateResponse:
        patch_roomba_state_response: PatchRoombaStateResponse = PatchRoombaStateResponse()
        try:
            roomba: Roomba | None = await self._session.scalar(select(Roomba).limit(1))
            if roomba is None:
                patch_roomba_state_response.response_code = ResponseCodes.NOT_FOUND
                patch_roomba_state_response.is_success = False
                patch_roomba_state_response.message = "Roomba not found"
            else:
                # Las columnas son TIMESTAMP WITHOUT TIME ZONE: si la fecha llega con
                # zona horaria hay que pasarla a UTC y quitarsela, igual que en el
                # repositorio del sensor de presencia.
                event_time = patch_roomba_state_request.event_time
                if event_time.tzinfo is not None:
                    event_time = event_time.astimezone(timezone.utc).replace(tzinfo = None)

                if patch_roomba_state_request.is_activation:
                    roomba.last_roomba_activation = event_time
                    roomba.last_target = patch_roomba_state_request.target.name

                if patch_roomba_state_request.is_finished:
                    roomba.last_roomba_end = event_time
                    roomba.last_clean_duration_minutes = (
                        0
                        if roomba.last_roomba_activation == datetime.min
                        else int((event_time - roomba.last_roomba_activation).total_seconds() // 60)
                    )

                if patch_roomba_state_request.battery_percent > 0:
                    roomba.battery_percentage = patch_roomba_state_request.battery_percent

                if not GenericUtils.is_null_or_empty(patch_roomba_state_request.pmap_id):
                    roomba.pmap_id = patch_roomba_state_request.pmap_id

                if not GenericUtils.is_null_or_empty(patch_roomba_state_request.user_pmapv_id):
                    roomba.user_pmapv_id = patch_roomba_state_request.user_pmapv_id

                roomba.phase = patch_roomba_state_request.phase.name
                roomba.bin_full = patch_roomba_state_request.bin_full
                roomba.error_code = patch_roomba_state_request.error_code
                roomba.error_message = patch_roomba_state_request.error_message
                roomba.is_online = patch_roomba_state_request.is_online
                roomba.last_seen = event_time

                await self._session.commit()

                patch_roomba_state_response.response_code = ResponseCodes.OK
                patch_roomba_state_response.is_success = True
                patch_roomba_state_response.message = f"Roomba state updated successfully (phase {roomba.phase}, battery {roomba.battery_percentage}%)."
        except Exception as ex:
            await self._session.rollback()
            patch_roomba_state_response.response_code = ResponseCodes.UNEXPECTED_ERROR
            patch_roomba_state_response.is_success = False
            patch_roomba_state_response.message = f"Unexpected error on RoombaRepository -> patch_roomba_state: {ex}"

        return patch_roomba_state_response
    #endregion
