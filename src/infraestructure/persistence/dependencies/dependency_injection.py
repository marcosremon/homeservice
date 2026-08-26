from fastapi import Depends
from sqlalchemy.ext.asyncio import AsyncSession
from application.interface.application.i_alexa_application import IAlexaApplication
from application.interface.application.i_change_computer_status_application import IChangeComputerStatusApplication
from application.interface.application.i_event_application import IEventApplication
from application.interface.application.i_presence_sensor_application import IPresenceSensorApplication
from application.interface.application.i_roomba_application import IRoombaApplication
from application.interface.repository.i_change_computer_status_repository import IChangeComputerStatusRepository
from application.interface.repository.i_event_repository import IEventRepository
from application.interface.repository.i_presence_sensor_repository import IPresenceSensorRepository
from application.interface.repository.i_roomba_repository import IRoombaRepository
from application.use_case.alexa_application import AlexaApplication
from application.use_case.change_computer_status_application import ChangeComputerStatusApplication
from application.use_case.event_application import EventApplication
from application.use_case.presence_sensor_application import PresenceSensorApplication
from application.use_case.roomba_application import RoombaApplication
from infraestructure.persistence.context.application_db_context import get_session
from infraestructure.persistence.repository.change_computer_status_repository import ChangeComputerStatusRepository
from infraestructure.persistence.repository.event_repository import EventRepository
from infraestructure.persistence.repository.presence_sensor_repository import PresenceSensorRepository
from infraestructure.persistence.repository.roomba_repository import RoombaRepository
from transversal.common.configuration.settings import Settings, get_settings

# region PresenceSensor
def get_presence_sensor_repository(session: AsyncSession = Depends(get_session)) -> IPresenceSensorRepository:
    return PresenceSensorRepository(session)

def get_presence_sensor_application(presence_sensor_repository: IPresenceSensorRepository = Depends(get_presence_sensor_repository)) -> IPresenceSensorApplication:
    return PresenceSensorApplication(presence_sensor_repository)
# endregion

# region Roomba
def build_roomba_repository(session: AsyncSession) -> IRoombaRepository:
    return RoombaRepository(session)

def get_roomba_repository(session: AsyncSession = Depends(get_session)) -> IRoombaRepository:
    return build_roomba_repository(session)

def get_roomba_application(roomba_repository: IRoombaRepository = Depends(get_roomba_repository)) -> IRoombaApplication:
    return RoombaApplication(roomba_repository)
# endregion

# region ChangeComputerStatus
def get_change_computer_status_repository(settings: Settings = Depends(get_settings)) -> IChangeComputerStatusRepository:
    return ChangeComputerStatusRepository(settings)

def get_change_computer_status_application(change_computer_status_repository: IChangeComputerStatusRepository = Depends(get_change_computer_status_repository)) -> IChangeComputerStatusApplication:
    return ChangeComputerStatusApplication(change_computer_status_repository)
# endregion

# region Event
def build_event_repository(session: AsyncSession) -> IEventRepository:
    """Fabrica sin Depends: la consume el monitor de presencia."""
    return EventRepository(session)

def get_event_repository(session: AsyncSession = Depends(get_session)) -> IEventRepository:
    return build_event_repository(session)

def get_event_application(event_repository: IEventRepository = Depends(get_event_repository)) -> IEventApplication:
    return EventApplication(event_repository)
# endregion

# region Alexa
def get_alexa_application() -> IAlexaApplication:
    return AlexaApplication()
# endregion