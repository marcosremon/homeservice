from contextlib import asynccontextmanager
from collections.abc import AsyncIterator

from fastapi import FastAPI

from infraestructure.background_tasks.job.RoombaActivationHandler import RoombaActivationHandler
from infraestructure.background_tasks.worker.PresenceSensorMonitor import PresenceSensorMonitor
from infraestructure.background_tasks.worker.RainSensorMonitor import RainSensorMonitor
from infraestructure.background_tasks.worker.RainSensorMonitor import RainSensorMonitor
from infraestructure.persistence.create_database.DatabaseMigrator import DatabaseMigrator
from service.web_api.controllers.alexa.AlexaController import router as alexa_router
from service.web_api.controllers.computer_status.ChangeComputerStatusController import router as change_computer_status_router
from service.web_api.controllers.roomba.RoombaController import router as roomba_router
from service.web_api.controllers.sensors.PresenceSensorController import router as presence_sensor_router
from service.web_api.controllers.sensors.RainSensorController import router as rain_sensor_router
from service.web_api.controllers.sensors.TemperatureController import router as temperature_sensor_router
from transversal.common.configuration.Settings import Settings, GetSettings

# Falla al arrancar si el .env o las variables de entorno estan incompletas,
# igual que la validacion de IOptions con ValidateOnStart() en ASP.NET.
GetSettings()

@asynccontextmanager
async def lifespan(_app: FastAPI) -> AsyncIterator[None]:
    if not await DatabaseMigrator.CreateOrUpdateDatabase():
        raise RuntimeError("No se pudo crear o actualizar la base de datos.")

    # Equivalente a AddHostedService<T>() de Program.cs. Aqui no hay host que los
    # gestione: se arrancan antes del yield y se paran despues, en orden inverso,
    # igual que hace el host de ASP.NET.
    presenceSensorMonitor: PresenceSensorMonitor = PresenceSensorMonitor()
    rainSensorMonitor: RainSensorMonitor = RainSensorMonitor()
    roombaActivationHandler: RoombaActivationHandler = RoombaActivationHandler()

    presenceSensorMonitor.Start()
    rainSensorMonitor.Start()
    roombaActivationHandler.Start()

    try:
        yield
    finally:
        await roombaActivationHandler.Stop()
        await rainSensorMonitor.Stop()
        await presenceSensorMonitor.Stop()

app: FastAPI = FastAPI(title = "HomeService API", lifespan = lifespan)
app.include_router(presence_sensor_router, prefix="/api")
app.include_router(temperature_sensor_router, prefix="/api")
app.include_router(rain_sensor_router, prefix = "/api")
app.include_router(roomba_router, prefix = "/api")
app.include_router(change_computer_status_router, prefix = "/api")
# El RoutePrefixConvention de Program.cs mete "api" delante de todos los
# controllers, alexa incluida: la URL del skill es /api/alexa.
app.include_router(alexa_router, prefix = "/api")

# Equivalente al bloque Kestrel de appsettings.json: `python -m service.web_api.Main`
# levanta el servidor con el host y el puerto del .env, sin pasarlos a mano.
if __name__ == "__main__":
    import uvicorn

    settings: Settings = GetSettings()
    uvicorn.run(app, host = settings.appHost, port = settings.appPort)