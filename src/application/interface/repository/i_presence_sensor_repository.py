from abc import ABC, abstractmethod

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


class IPresenceSensorRepository(ABC):
    @abstractmethod
    async def create_presence_sensor(
        self, create_presence_sensor_request: CreatePresenceSensorRequest
    ) -> CreatePresenceSensorResponse: ...
