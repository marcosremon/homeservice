import asyncio
from contextlib import asynccontextmanager

from application.data_transfer_object.roomba.patch_roomba_state.patch_roomba_state_request import PatchRoombaStateRequest
from application.data_transfer_object.roomba.patch_roomba_state.patch_roomba_state_response import PatchRoombaStateResponse
from application.event.roomba_activated_event import RoombaActivatedEvent
from application.interface.repository.i_roomba_repository import IRoombaRepository
from infraestructure.persistence.context.application_db_context import get_session
from infraestructure.persistence.dependencies.dependency_injection import build_roomba_repository

class RoombaActivationHandler:
    def __init__(self) -> None:
        self._pending: set[asyncio.Task[None]] = set()

    # region start
    def start(self) -> None:
        RoombaActivatedEvent.subscribe(self._handle_roomba_activation)
    # endregion

    # region stop
    async def stop(self) -> None:
        RoombaActivatedEvent.unsubscribe(self._handle_roomba_activation)
        if self._pending:
            await asyncio.gather(*self._pending, return_exceptions = True)
    # endregion

    # region _handle_roomba_activation
    def _handle_roomba_activation(self, patch_roomba_state_request: PatchRoombaStateRequest) -> None:
        """
        Callback sincrono del evento: crea la tarea y guarda la referencia.
        Sin guardarla el recolector de basura puede cargarse la tarea a medias.
        """
        task: asyncio.Task[None] = asyncio.create_task(self._patch_roomba_state(patch_roomba_state_request))
        self._pending.add(task)
        task.add_done_callback(self._pending.discard)
    # endregion

    # region _patch_roomba_state
    async def _patch_roomba_state(self, patch_roomba_state_request: PatchRoombaStateRequest) -> None:
        try:
            async with asynccontextmanager(get_session)() as session:
                roomba_repository: IRoombaRepository = build_roomba_repository(session)

                patch_roomba_state_response: PatchRoombaStateResponse = await roomba_repository.patch_roomba_state(patch_roomba_state_request)
                if not patch_roomba_state_response.is_success:
                    print(f"RoombaActivationHandler -> {patch_roomba_state_response.message}")
        except Exception as ex:
            print(f"RoombaActivationHandler -> _handle_roomba_activation -> {ex}")
    # endregion