from abc import ABC, abstractmethod

from application.data_transfer_object.roomba.create_roomba.create_roomba_request import CreateRoombaRequest
from application.data_transfer_object.roomba.create_roomba.create_roomba_response import CreateRoombaResponse
from application.data_transfer_object.roomba.patch_roomba_state.patch_roomba_state_request import PatchRoombaStateRequest
from application.data_transfer_object.roomba.patch_roomba_state.patch_roomba_state_response import PatchRoombaStateResponse

class IRoombaRepository(ABC):
    @abstractmethod
    async def create_roomba(self, create_roomba_request: CreateRoombaRequest) -> CreateRoombaResponse: ...

    @abstractmethod
    async def patch_roomba_state(self, patch_roomba_state_request: PatchRoombaStateRequest) -> PatchRoombaStateResponse: ...
