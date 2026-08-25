from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.create_presence_sensor_request import (
    CreatePresenceSensorRequest,
)
from application.data_transfer_object.home_automation.sensor.presence_sensor.create_presence_sensor.create_presence_sensor_response import (
    CreatePresenceSensorResponse,
)
from application.data_transfer_object.home_automation.sensor.presence_sensor.patch_presence_sensor_data.patch_presence_sensor_data_request import (
    PatchPresenceSensorDataRequest,
)
from application.data_transfer_object.home_automation.sensor.presence_sensor.patch_presence_sensor_data.patch_presence_sensor_data_response import (
    PatchPresenceSensorDataResponse,
)
from application.interface.application.i_presence_sensor_application import IPresenceSensorApplication
from application.interface.repository.i_presence_sensor_repository import IPresenceSensorRepository


class PresenceSensorApplication(IPresenceSensorApplication):
    def __init__(self, presence_sensor_repository: IPresenceSensorRepository):
        self._presence_sensor_repository = presence_sensor_repository

    async def create_presence_sensor(self, create_presence_sensor_request: CreatePresenceSensorRequest) -> CreatePresenceSensorResponse:
        return await self._presence_sensor_repository.create_presence_sensor(create_presence_sensor_request)

    async def patch_presence_sensor_data(self, patch_presence_sensor_data_request: PatchPresenceSensorDataRequest) -> PatchPresenceSensorDataResponse:
        return await self._presence_sensor_repository.patch_presence_sensor_data(patch_presence_sensor_data_request)