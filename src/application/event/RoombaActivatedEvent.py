from collections.abc import Callable
from typing import TypeAlias

from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateRequest import PatchRoombaStateRequest

RoombaActivatedHandler: TypeAlias = Callable[[PatchRoombaStateRequest], None]

class RoombaActivatedEvent:
    _handlers: list[RoombaActivatedHandler] = []

    # region subscribe
    @staticmethod
    def Subscribe(handler: RoombaActivatedHandler) -> None:
        if handler not in RoombaActivatedEvent._handlers:
            RoombaActivatedEvent._handlers.append(handler)
    # endregion

    # region unsubscribe
    @staticmethod
    def Unsubscribe(handler: RoombaActivatedHandler) -> None:
        if handler in RoombaActivatedEvent._handlers:
            RoombaActivatedEvent._handlers.remove(handler)
    # endregion

    # region publish
    @staticmethod
    def Publish(patchRoombaStateRequest: PatchRoombaStateRequest) -> None:
        for handler in list(RoombaActivatedEvent._handlers):
            try:
                handler(patchRoombaStateRequest)
            except Exception as ex:
                print(f"RoombaActivatedEvent -> publish -> {ex}")
    # endregion