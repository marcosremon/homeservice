from application.data_transfer_object.roomba.create_roomba.create_roomba_request import CreateRoombaRequest
from application.data_transfer_object.roomba.create_roomba.create_roomba_response import CreateRoombaResponse
from application.data_transfer_object.roomba.patch_roomba_state.patch_roomba_state_request import PatchRoombaStateRequest
from application.data_transfer_object.roomba.patch_roomba_state.patch_roomba_state_response import PatchRoombaStateResponse
from application.interface.application.i_roomba_application import IRoombaApplication
from application.interface.repository.i_roomba_repository import IRoombaRepository

class RoombaApplication(IRoombaApplication):

    def __init__(self, roomba_repository: IRoombaRepository):
        self._roomba_repository = roomba_repository

    async def create_roomba(self, create_roomba_request: CreateRoombaRequest) -> CreateRoombaResponse:
        return await self._roomba_repository.create_roomba(create_roomba_request)

    async def patch_roomba_state(self, patch_roomba_state_request: PatchRoombaStateRequest) -> PatchRoombaStateResponse:
        return await self._roomba_repository.patch_roomba_state(patch_roomba_state_request)