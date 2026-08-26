from contextlib import asynccontextmanager
from collections.abc import AsyncIterator

from fastapi import FastAPI

from infraestructure.background_tasks.job.roomba_activation_handler import RoombaActivationHandler
from infraestructure.background_tasks.worker.presence_sensor_monitor import PresenceSensorMonitor
from infraestructure.persistence.create_database.database_migrator import create_or_update_database
from service.web_api.controllers.alexa.alexa_controller import router as alexa_router
from service.web_api.controllers.computer_status.change_computer_status_controller import router as change_computer_status_router
from service.web_api.controllers.roomba.roomba_controller import router as roomba_router
from service.web_api.controllers.sensors.presence_sensor_controller import router as presence_sensor_router
from transversal.common.configuration.settings import get_settings

# Falla al arrancar si el .env o las variables de entorno estan incompletas,
# igual que la validacion de IOptions con ValidateOnStart() en ASP.NET.
get_settings()

@asynccontextmanager
async def lifespan(_app: FastAPI) -> AsyncIterator[None]:
    if not await create_or_update_database():
        raise RuntimeError("No se pudo crear o actualizar la base de datos.")

    # Equivalente a AddHostedService<T>() de Program.cs. Aqui no hay host que los
    # gestione: se arrancan antes del yield y se paran despues, y el orden de
    # parada es el inverso al de arranque, igual que hace el host de ASP.NET.
    background_services: list[PresenceSensorMonitor | RoombaActivationHandler] = [
        PresenceSensorMonitor(),
        RoombaActivationHandler(),
    ]
    for background_service in background_services:
        background_service.start()

    try:
        yield
    finally:
        for background_service in reversed(background_services):
            await background_service.stop()

app = FastAPI(title="HomeService API", lifespan=lifespan)
app.include_router(presence_sensor_router, prefix="/api")
app.include_router(roomba_router, prefix="/api")
app.include_router(change_computer_status_router, prefix="/api")
# Alexa va sin el prefijo /api: la URL del endpoint del skill es la que espera Amazon.
app.include_router(alexa_router)