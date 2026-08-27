import asyncio
from contextlib import asynccontextmanager
from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateRequest import PatchRoombaStateRequest
from application.data_transfer_object.roomba.patch_roomba_state.PatchRoombaStateResponse import PatchRoombaStateResponse
from application.event.RoombaActivatedEvent import RoombaActivatedEvent
from application.interface.repository.IRoombaRepository import IRoombaRepository
from infraestructure.persistence.context.ApplicationDbContext import GetSession
from infraestructure.persistence.dependencies.DependencyInjection import BuildRoombaRepository

class RoombaActivationHandler:
    def __init__(self) -> None:
        self._pending: set[asyncio.Task[None]] = set()

    # region start
    def Start(self) -> None:
        RoombaActivatedEvent.Subscribe(self._handleRoombaActivation)
    # endregion

    # region stop
    async def Stop(self) -> None:
        RoombaActivatedEvent.Unsubscribe(self._handleRoombaActivation)
        if self._pending:
            await asyncio.gather(*self._pending, return_exceptions = True)
    # endregion

    # region _handle_roomba_activation
    def _handleRoombaActivation(self, patchRoombaStateRequest: PatchRoombaStateRequest) -> None:
        """
        Callback sincrono del evento: crea la tarea y guarda la referencia.
        Sin guardarla el recolector de basura puede cargarse la tarea a medias.
        """
        task: asyncio.Task[None] = asyncio.create_task(self._patchRoombaState(patchRoombaStateRequest))
        self._pending.add(task)
        task.add_done_callback(self._pending.discard)
    # endregion

    # region _patch_roomba_state
    @staticmethod
    async def _patchRoombaState(patchRoombaStateRequest: PatchRoombaStateRequest) -> None:
        try:
            async with asynccontextmanager(GetSession)() as session:
                roombaRepository: IRoombaRepository = BuildRoombaRepository(session)

                patchRoombaStateResponse: PatchRoombaStateResponse = await roombaRepository.PatchRoombaState(patchRoombaStateRequest)
                if not patchRoombaStateResponse.isSuccess:
                    print(f"RoombaActivationHandler -> {patchRoombaStateResponse.message}")
        except Exception as ex:
            print(f"RoombaActivationHandler -> _handle_roomba_activation -> {ex}")
    # endregion