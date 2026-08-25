from contextlib import asynccontextmanager
from collections.abc import AsyncIterator

from fastapi import FastAPI

from infraestructure.persistence.create_database.database_migrator import create_or_update_database
from service.web_api.controllers.sensors.presence_sensor_controller import router as presence_sensor_router
from transversal.common.configuration.settings import get_settings

# Falla al arrancar si el .env o las variables de entorno estan incompletas,
# igual que la validacion de IOptions con ValidateOnStart() en ASP.NET.
get_settings()


@asynccontextmanager
async def lifespan(_app: FastAPI) -> AsyncIterator[None]:
    """Equivalente al DatabaseMigrator.CreateOrUpdateDatabase(...) de Program.cs."""
    if not await create_or_update_database():
        raise RuntimeError("No se pudo crear o actualizar la base de datos.")

    yield


app = FastAPI(title="HomeService API", lifespan=lifespan)

# prefix="/api" == RoutePrefixConvention("api") de Program.cs
app.include_router(presence_sensor_router, prefix="/api")
