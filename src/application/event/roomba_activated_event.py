from collections.abc import Callable

from application.data_transfer_object.roomba.patch_roomba_state.patch_roomba_state_request import PatchRoombaStateRequest

RoombaActivatedHandler = Callable[[PatchRoombaStateRequest], None]

class RoombaActivatedEvent:
    _handlers: list[RoombaActivatedHandler] = []

    # region subscribe
    @staticmethod
    def subscribe(handler: RoombaActivatedHandler) -> None:
        if handler not in RoombaActivatedEvent._handlers:
            RoombaActivatedEvent._handlers.append(handler)
    # endregion

    # region unsubscribe
    @staticmethod
    def unsubscribe(handler: RoombaActivatedHandler) -> None:
        if handler in RoombaActivatedEvent._handlers:
            RoombaActivatedEvent._handlers.remove(handler)
    # endregion

    # region publish
    @staticmethod
    def publish(patch_roomba_state_request: PatchRoombaStateRequest) -> None:
        for handler in list(RoombaActivatedEvent._handlers):
            try:
                handler(patch_roomba_state_request)
            except Exception as ex:
                print(f"RoombaActivatedEvent -> publish -> {ex}")
    # endregion