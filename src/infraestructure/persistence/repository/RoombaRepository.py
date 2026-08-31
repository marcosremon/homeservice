from datetime import datetime
import fastapi
from sqlalchemy import select
from sqlalchemy.ext.asyncio import AsyncSession
from application.data_transfer_object.roomba.create_roomba.CreateRoombaRequest import CreateRoombaRequest
from application.data_transfer_object.roomba.create_roomba.CreateRoombaResponse import CreateRoombaResponse
from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateRequest import PatchRoombaStateRequest
from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateResponse import PatchRoombaStateResponse
from application.interface.repository.IRoombaRepository import IRoombaRepository
from domain.model.entity.Device import Device
from domain.model.entity.HouseZone import HouseZone
from domain.model.entity.Roomba import Roomba
from transversal.common.utils.GeneralUtils import GeneralUtils
from transversal.common.wrappers.base.ResponseCodes import ResponseCodes

class RoombaRepository(IRoombaRepository):

    def __init__(self, session: AsyncSession):
        self._session: AsyncSession = session

    # region create_roomba
    async def CreateRoomba(self, createRoombaRequest: CreateRoombaRequest) -> CreateRoombaResponse:
        createRoombaResponse: CreateRoombaResponse = CreateRoombaResponse()
        try:
            anyRoomba: Roomba | None = await self._session.scalar(select(Roomba).limit(1))
            if anyRoomba is not None:
                createRoombaResponse.responseCode = ResponseCodes.ANY_ROOMBA_EXIST
                createRoombaResponse.isSuccess = False
                createRoombaResponse.message = "Any roomba exists on bbdd"
            else:
                foundHouseZone: HouseZone | None = await self._session.scalar(select(HouseZone)
                    .where(HouseZone.callout == createRoombaRequest.callout))

                if foundHouseZone is not None:
                    houseZone: HouseZone = foundHouseZone
                else:
                    houseZone = HouseZone(
                        callout = createRoombaRequest.callout
                    )

                    self._session.add(houseZone)
                    await self._session.flush()

                foundDevice: Device | None = await self._session.scalar(select(Device).where(
                    Device.houseZoneId == houseZone.houseZoneId,
                    Device.deviceName == createRoombaRequest.deviceName,
                    Device.deviceType == createRoombaRequest.deviceType
                ))

                if foundDevice is not None:
                    device: Device = foundDevice
                else:
                    device = Device(
                        houseZoneId = houseZone.houseZoneId,
                        deviceName = createRoombaRequest.deviceName,
                        deviceType = createRoombaRequest.deviceType,
                        model = createRoombaRequest.model,
                        manufacturer = createRoombaRequest.manufacturer,
                        macAddress = createRoombaRequest.macAddress,
                    )

                    self._session.add(device)
                    await self._session.flush()

                roomba: Roomba | None = await self._session.scalar(select(Roomba).where(Roomba.deviceId == device.deviceId))
                if roomba is not None:
                    createRoombaResponse.responseCode = ResponseCodes.ANY_ROOMBA_EXIST
                    createRoombaResponse.isSuccess = True
                    createRoombaResponse.message = "roomba exist"

                    await self._session.rollback()
                else:
                    roomba = Roomba(
                        deviceId = device.deviceId,
                        lastRoombaActivation = datetime.min,
                        lastTarget = "",
                        lastRoombaEnd = datetime.min,
                        lastSeen = datetime.min,
                    )

                    self._session.add(roomba)
                    await self._session.commit()

                    createRoombaResponse.responseCode = ResponseCodes.CREATED
                    createRoombaResponse.isSuccess = True
                    createRoombaResponse.message = "roomba created"
        except Exception as ex:
            await self._session.rollback()

            createRoombaResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            createRoombaResponse.isSuccess = False
            createRoombaResponse.message = f"Unexpected error on RoombaRepository -> create_roomba: {ex}"

        return createRoombaResponse
    # endregion

    # region patch_roomba_state
    async def PatchRoombaState(self, patchRoombaStateRequest: PatchRoombaStateRequest) -> PatchRoombaStateResponse:
        patchRoombaStateResponse: PatchRoombaStateResponse = PatchRoombaStateResponse()
        try:
            roomba: Roomba | None = await self._session.scalar(select(Roomba).limit(1))
            if roomba is None:
                patchRoombaStateResponse.responseCode = ResponseCodes.NOT_FOUND
                patchRoombaStateResponse.isSuccess = False
                patchRoombaStateResponse.message = "roomba not found"
            else:
                # La hora del evento la pone el servidor al procesar la peticion,
                # no la que mande el dispositivo.
                eventTime: datetime = GeneralUtils.UtcNow()

                if patchRoombaStateRequest.isActivation:
                    roomba.lastRoombaActivation = eventTime
                    roomba.lastTarget = patchRoombaStateRequest.target.name

                if patchRoombaStateRequest.isFinished:
                    roomba.lastRoombaEnd = eventTime
                    roomba.lastCleanDurationMinutes = (
                        0
                        if roomba.lastRoombaActivation == datetime.min
                        else int((eventTime - roomba.lastRoombaActivation).total_seconds() // 60)
                    )

                if patchRoombaStateRequest.batteryPercent > 0:
                    roomba.batteryPercent = patchRoombaStateRequest.batteryPercent

                if not GeneralUtils.IsNullOrEmpty(patchRoombaStateRequest.pmapId):
                    roomba.pmapId = patchRoombaStateRequest.pmapId

                if not GeneralUtils.IsNullOrEmpty(patchRoombaStateRequest.userPmapvId):
                    roomba.userPmapvId = patchRoombaStateRequest.userPmapvId

                roomba.phase = patchRoombaStateRequest.phase.name
                roomba.binFull = patchRoombaStateRequest.binFull
                roomba.errorCode = patchRoombaStateRequest.errorCode
                roomba.errorMessage = patchRoombaStateRequest.errorMessage
                roomba.isOnline = patchRoombaStateRequest.isOnline
                roomba.lastSeen = eventTime

                await self._session.commit()

                patchRoombaStateResponse.responseCode = ResponseCodes.OK
                patchRoombaStateResponse.isSuccess = True
                patchRoombaStateResponse.message = f"roomba state updated successfully (phase {roomba.phase}, battery {roomba.batteryPercent}%)."
        except Exception as ex:
            await self._session.rollback()

            patchRoombaStateResponse.responseCode = ResponseCodes.UNEXPECTED_ERROR
            patchRoombaStateResponse.isSuccess = False
            patchRoombaStateResponse.message = f"Unexpected error on RoombaRepository -> patch_roomba_state: {ex}"

        return patchRoombaStateResponse
    # endregion