from application.data_transfer_object.roomba.create_roomba.CreateRoombaRequest import CreateRoombaRequest
from application.data_transfer_object.roomba.create_roomba.CreateRoombaResponse import CreateRoombaResponse
from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateRequest import PatchRoombaStateRequest
from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateResponse import PatchRoombaStateResponse
from application.interface.application.IRoombaApplication import IRoombaApplication
from application.interface.repository.IRoombaRepository import IRoombaRepository

class RoombaApplication(IRoombaApplication):

    def __init__(self, roombaRepository: IRoombaRepository):
        self._roombaRepository = roombaRepository

    async def CreateRoomba(self, createRoombaRequest: CreateRoombaRequest) -> CreateRoombaResponse:
        return await self._roombaRepository.CreateRoomba(createRoombaRequest)

    async def PatchRoombaState(self, patchRoombaStateRequest: PatchRoombaStateRequest) -> PatchRoombaStateResponse:
        return await self._roombaRepository.PatchRoombaState(patchRoombaStateRequest)