import asyncio
import sys
from pathlib import Path

# Los imports del proyecto son absolutos desde src/, asi que hay que meterlo en
# el path antes de importar nada (equivalente a la referencia de proyecto en C#).
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

from domain.model.enum.roomba.RoombaAction import RoombaAction  # noqa: E402
from domain.model.enum.roomba.RoombaPhase import RoombaPhase  # noqa: E402
from domain.model.enum.roomba.RoombaTarget import RoombaTarget  # noqa: E402
from infraestructure.background_tasks.job.RoombaActivationHandler import RoombaActivationHandler  # noqa: E402
from infraestructure.gateway.roomba.RoombaUtils import RoombaUtils  # noqa: E402
from transversal.common.configuration.Settings import Settings, GetSettings  # noqa: E402

PERSIST_TO_DATABASE: bool = True

TARGET: RoombaTarget = RoombaTarget.LIVING_ROOM
ACTION: str = "home"

async def Main() -> None:
    settings: Settings = GetSettings()
    print(f"--- Configuracion cargada: roomba {settings.roombaId}:{settings.roombaPort} blid {settings.roombaBlid} ---")

    roombaActivationHandler: RoombaActivationHandler | None = None
    if PERSIST_TO_DATABASE:
        roombaActivationHandler = RoombaActivationHandler()
        roombaActivationHandler.Start()

    try:
        # Parar la roomba a mitad es lanzar esto otra vez con `pause` o `home`:
        # matar el proceso no la detiene, la orden ya se publico por MQTT y el
        # aparato sigue a lo suyo.
        action: str = sys.argv[1].lower() if len(sys.argv) > 1 else ACTION
        print(f"--- Accion: {action} ---")

        match action:
            case "start": print(await RoombaUtils.StartRoomba(TARGET))
            case "pause": print(await RoombaUtils.PauseRoomba())
            case "resume": print(await RoombaUtils.ReactiveRoomba(TARGET))
            case "home": print(await RoombaUtils.SendRoombaHome())
            case "phase":
                phase: RoombaPhase | None = await RoombaUtils.GetRoombaPhase()
                print(f"phase -> {phase.name if phase is not None else 'sin respuesta'}")
            case "rooms": await RoombaUtils.GetRoomInfo()
            case "raw": await RoombaUtils.SendRoombaOrder(RoombaAction.START, TARGET)
            case _: print(f"Accion no reconocida: {action}")
    finally:
        # Sin esto el proceso muere antes de que el handler acabe el UPDATE.
        if roombaActivationHandler is not None:
            await roombaActivationHandler.Stop()

if __name__ == "__main__":
    try:
        asyncio.run(Main())
    except KeyboardInterrupt:
        print("\nInterrumpido.")
    except Exception as ex:
        print(f"Error critico en Main: {ex}")

    print("\nProceso finalizado.")
