from abc import ABC, abstractmethod

from application.data_transfer_object.roomba.create_roomba.CreateRoombaRequest import CreateRoombaRequest
from application.data_transfer_object.roomba.create_roomba.CreateRoombaResponse import CreateRoombaResponse
from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateRequest import PatchRoombaStateRequest
from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateResponse import PatchRoombaStateResponse

class IRoombaApplication(ABC):
    @abstractmethod
    async def CreateRoomba(self, createRoombaRequest: CreateRoombaRequest) -> CreateRoombaResponse: ...

    @abstractmethod
    async def PatchRoombaState(self, patchRoombaStateRequest: PatchRoombaStateRequest) -> PatchRoombaStateResponse: ...