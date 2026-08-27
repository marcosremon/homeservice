from fastapi import APIRouter, Depends
from fastapi_utils.cbv import cbv

from application.interface.application.ITemperatureSensorApplication import ITemperatureSensorApplication
from infraestructure.persistence.dependencies.DependencyInjection import GetTemperatureSensorApplication
from transversal.security.filter.ApiKeyAuth import ApiKeyAuth

router: APIRouter = APIRouter(
    prefix="/sensors/temperature-sensor",
    dependencies=[Depends(ApiKeyAuth.GetApiKey)],  # equivalente a [ApiKeyAuth] a nivel de clase
)

@cbv(router)
class TemperatureController:
    _presenceSensorApplication: ITemperatureSensorApplication = Depends(GetTemperatureSensorApplication)