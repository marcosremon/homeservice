from functools import lru_cache
import httpx
from fastapi import Depends
from sqlalchemy.ext.asyncio import AsyncSession
from application.interface.application.IAlexaApplication import IAlexaApplication
from application.interface.application.IChangeComputerStatusApplication import IChangeComputerStatusApplication
from application.interface.application.IEventApplication import IEventApplication
from application.interface.application.IPresenceSensorApplication import IPresenceSensorApplication
from application.interface.application.IRainSensorApplication import IRainSensorApplication
from application.interface.application.IRoombaApplication import IRoombaApplication
from application.interface.repository.IChangeComputerStatusRepository import IChangeComputerStatusRepository
from application.interface.repository.IEventRepository import IEventRepository
from application.interface.repository.ITemperatureSensorRepository import ITemperatureSensorRepository
from application.interface.repository.IPresenceSensorRepository import IPresenceSensorRepository
from application.interface.repository.IRainSensorRepository import IRainSensorRepository
from application.interface.repository.IRoombaRepository import IRoombaRepository
from application.interface.repository.ILightRepository import ILightRepository
from application.interface.application.ITemperatureSensorApplication import ITemperatureSensorApplication
from application.interface.service.IAlexaService import IAlexaService
from application.interface.service.IComputerStatusService import IComputerStatusService
from application.interface.service.IGeminiService import IGeminiService
from application.interface.service.ILightService import ILightService
from application.interface.service.IMqttService import IMqttService
from application.interface.service.INotificationService import INotificationService
from application.interface.service.IRoombaService import IRoombaService
from application.interface.service.ITemperatureSensorService import ITemperatureSensorService
from application.use_case.AlexaApplication import AlexaApplication
from application.use_case.ChangeComputerStatusApplication import ChangeComputerStatusApplication
from application.use_case.EventApplication import EventApplication
from application.use_case.PresenceSensorApplication import PresenceSensorApplication
from application.use_case.RainSensorApplication import RainSensorApplication
from application.use_case.RoombaApplication import RoombaApplication
from application.use_case.TemperatureSensorApplication import TemperatureSensorApplication
from infraestructure.persistence.context.ApplicationDbContext import GetSession
from infraestructure.persistence.repository.ChangeComputerStatusRepository import ChangeComputerStatusRepository
from infraestructure.persistence.repository.EventRepository import EventRepository
from infraestructure.gateway.AlexaService import AlexaService
from infraestructure.gateway.ComputerStatusService import ComputerStatusService
from infraestructure.gateway.GeminiService import GeminiService
from infraestructure.gateway.LightService import LightService
from infraestructure.gateway.MqttService import MqttService
from infraestructure.gateway.NotificationService import NotificationService
from infraestructure.gateway.RoombaService import RoombaService
from infraestructure.gateway.TemperatureSensorService import TemperatureSensorService
from infraestructure.persistence.repository.LightRepository import LightRepository
from infraestructure.persistence.repository.PresenceSensorRepository import PresenceSensorRepository
from infraestructure.persistence.repository.RainSensorRepository import RainSensorRepository
from infraestructure.persistence.repository.RoombaRepository import RoombaRepository
from infraestructure.persistence.repository.TemperatureSensorRepository import TemperatureSensorRepository
from transversal.common.configuration.Settings import Settings, GetSettings

# region PresenceSensor
def GetPresenceSensorRepository(session: AsyncSession = Depends(GetSession)) -> IPresenceSensorRepository:
    return PresenceSensorRepository(session)

def GetPresenceSensorApplication(presenceSensorRepository: IPresenceSensorRepository = Depends(GetPresenceSensorRepository)) -> IPresenceSensorApplication:
    return PresenceSensorApplication(presenceSensorRepository)
# endregion

# region TemperatureSensor
def GetTemperatureSensorRepository(session: AsyncSession = Depends(GetSession)) -> ITemperatureSensorRepository:
    return TemperatureSensorRepository(session)

def GetTemperatureSensorApplication(temperatureSensorRepository: ITemperatureSensorRepository = Depends(GetTemperatureSensorRepository)) -> ITemperatureSensorApplication:
    return TemperatureSensorApplication(temperatureSensorRepository)
# endregion

# region RainSensor
def BuildRainSensorRepository(session: AsyncSession) -> IRainSensorRepository:
    """Fabrica sin Depends: la consume el monitor de lluvia."""
    return RainSensorRepository(session)

def GetRainSensorRepository(session: AsyncSession = Depends(GetSession)) -> IRainSensorRepository:
    return BuildRainSensorRepository(session)

def GetRainSensorApplication(rainSensorRepository: IRainSensorRepository = Depends(GetRainSensorRepository)) -> IRainSensorApplication:
    return RainSensorApplication(rainSensorRepository, BuildNotificationService())
# endregion

# region roomba
def BuildRoombaRepository(session: AsyncSession) -> IRoombaRepository:
    return RoombaRepository(session)

def GetRoombaRepository(session: AsyncSession = Depends(GetSession)) -> IRoombaRepository:
    return BuildRoombaRepository(session)

def GetRoombaApplication(roombaRepository: IRoombaRepository = Depends(GetRoombaRepository)) -> IRoombaApplication:
    return RoombaApplication(roombaRepository)
# endregion

# region ChangeComputerStatus
def GetChangeComputerStatusRepository(settings: Settings = Depends(GetSettings)) -> IChangeComputerStatusRepository:
    return ChangeComputerStatusRepository(settings)

def GetChangeComputerStatusApplication(changeComputerStatusRepository: IChangeComputerStatusRepository = Depends(GetChangeComputerStatusRepository)) -> IChangeComputerStatusApplication:
    return ChangeComputerStatusApplication(changeComputerStatusRepository)
# endregion

# region Event
def BuildEventRepository(session: AsyncSession) -> IEventRepository:
    """Fabrica sin Depends: la consume el monitor de presencia."""
    return EventRepository(session)

def GetEventRepository(session: AsyncSession = Depends(GetSession)) -> IEventRepository:
    return BuildEventRepository(session)

def GetEventApplication(eventRepository: IEventRepository = Depends(GetEventRepository)) -> IEventApplication:
    return EventApplication(eventRepository)
# endregion

# region light
def BuildLightRepository(session: AsyncSession) -> ILightRepository:
    return LightRepository(session)

def GetLightRepository(session: AsyncSession = Depends(GetSession)) -> ILightRepository:
    return BuildLightRepository(session)
# endregion

# region Services
@lru_cache
def _GetHttpClient() -> httpx.AsyncClient:
    """Equivalente a IHttpClientFactory: un cliente reutilizado, no uno por peticion."""
    return httpx.AsyncClient()

def BuildNotificationService() -> INotificationService:
    """Fabrica sin Depends: la consumen la application y los monitores."""
    return NotificationService(_GetHttpClient(), GetSettings())

def GetNotificationService() -> INotificationService:
    return BuildNotificationService()

@lru_cache
def GetMqttService() -> IMqttService:
    """Singleton: la conexion con el broker se comparte entre peticiones."""
    return MqttService(GetSettings())

def GetLightService(lightRepository: ILightRepository = Depends(GetLightRepository)) -> ILightService:
    return LightService(GetMqttService(), lightRepository)

def GetRoombaService() -> IRoombaService:
    return RoombaService()

def GetComputerStatusService(settings: Settings = Depends(GetSettings)) -> IComputerStatusService:
    return ComputerStatusService(settings)

def GetGeminiService(settings: Settings = Depends(GetSettings)) -> IGeminiService:
    return GeminiService(_GetHttpClient(), settings)

def GetTemperatureSensorService(settings: Settings = Depends(GetSettings)) -> ITemperatureSensorService:
    return TemperatureSensorService(GetMqttService(), settings)

def GetAlexaService(
    lightService: ILightService = Depends(GetLightService),
    roombaService: IRoombaService = Depends(GetRoombaService),
    geminiService: IGeminiService = Depends(GetGeminiService),
    computerStatusService: IComputerStatusService = Depends(GetComputerStatusService),
    temperatureSensorService: ITemperatureSensorService = Depends(GetTemperatureSensorService),
    settings: Settings = Depends(GetSettings),
) -> IAlexaService:
    return AlexaService(lightService, roombaService, geminiService, computerStatusService, temperatureSensorService, settings)
# endregion

# region alexa
def GetAlexaApplication(alexaService: IAlexaService = Depends(GetAlexaService)) -> IAlexaApplication:
    return AlexaApplication(alexaService)
# endregion