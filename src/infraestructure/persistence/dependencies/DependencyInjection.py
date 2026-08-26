from fastapi import Depends
from sqlalchemy.ext.asyncio import AsyncSession
from application.interface.application.IAlexaApplication import IAlexaApplication
from application.interface.application.IChangeComputerStatusApplication import IChangeComputerStatusApplication
from application.interface.application.IEventApplication import IEventApplication
from application.interface.application.IPresenceSensorApplication import IPresenceSensorApplication
from application.interface.application.IRoombaApplication import IRoombaApplication
from application.interface.repository.IChangeComputerStatusRepository import IChangeComputerStatusRepository
from application.interface.repository.IEventRepository import IEventRepository
from application.interface.repository.IPresenceSensorRepository import IPresenceSensorRepository
from application.interface.repository.IRoombaRepository import IRoombaRepository
from application.use_case.AlexaApplication import AlexaApplication
from application.use_case.ChangeComputerStatusApplication import ChangeComputerStatusApplication
from application.use_case.EventApplication import EventApplication
from application.use_case.PresenceSensorApplication import PresenceSensorApplication
from application.use_case.RoombaApplication import RoombaApplication
from infraestructure.persistence.context.ApplicationDbContext import GetSession
from infraestructure.persistence.repository.ChangeComputerStatusRepository import ChangeComputerStatusRepository
from infraestructure.persistence.repository.EventRepository import EventRepository
from infraestructure.persistence.repository.PresenceSensorRepository import PresenceSensorRepository
from infraestructure.persistence.repository.RoombaRepository import RoombaRepository
from transversal.common.configuration.Settings import Settings, GetSettings

# region PresenceSensor
def GetPresenceSensorRepository(session: AsyncSession = Depends(GetSession)) -> IPresenceSensorRepository:
    return PresenceSensorRepository(session)

def GetPresenceSensorApplication(presenceSensorRepository: IPresenceSensorRepository = Depends(GetPresenceSensorRepository)) -> IPresenceSensorApplication:
    return PresenceSensorApplication(presenceSensorRepository)
# endregion

# region Roomba
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

# region Alexa
def GetAlexaApplication() -> IAlexaApplication:
    return AlexaApplication()
# endregion