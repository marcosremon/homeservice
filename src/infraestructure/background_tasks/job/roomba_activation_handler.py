import asyncio
from contextlib import asynccontextmanager

from infraestructure.persistence.context.application_db_context import get_session

class RoombaActivationHandler:
    def __init__(self) -> None:
        self._pending: set[asyncio.Task[None]] = set()

    # region start
    def start(self) -> None:
        # TODO: RoombaActivatedEvent.subscribe(self._handle_roomba_activation)
        # cuando exista el modulo de eventos.
        pass
    # endregion

    # region stop
    async def stop(self) -> None:
        # TODO: RoombaActivatedEvent.unsubscribe(self._handle_roomba_activation)
        if self._pending:
            await asyncio.gather(*self._pending, return_exceptions = True)
    # endregion

    # region _handle_roomba_activation
    def _handle_roomba_activation(self, patch_roomba_state_request: object) -> None:
        """Callback sincrono del evento: crea la tarea y guarda la referencia.

        Sin guardarla el recolector de basura puede cargarse la tarea a medias.
        """
        task = asyncio.create_task(self._patch_roomba_state(patch_roomba_state_request))
        self._pending.add(task)
        task.add_done_callback(self._pending.discard)
    # endregion

    # region _patch_roomba_state
    async def _patch_roomba_state(self, patch_roomba_state_request: object) -> None:
        try:
            # Equivalente a serviceProvider.CreateScope() + GetRequiredService<IRoombaRepository>().
            async with asynccontextmanager(get_session)() as session:
                # TODO: RoombaRepository(session).patch_roomba_state(patch_roomba_state_request)
                # y loguear el mensaje si no is_success. El repositorio aun no existe.
                _ = session, patch_roomba_state_request
        except Exception as ex:
            print(f"RoombaActivationHandler -> _handle_roomba_activation -> {ex}")
    # endregion