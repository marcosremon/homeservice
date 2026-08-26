from pydantic import Field
from pydantic.dataclasses import dataclass

from infraestructure.gateway.roomba.payload.RoombaRegion import RoombaRegion
from transversal.common.configuration.JsonModelConfig import JSON_MODEL_CONFIG

@dataclass(config = JSON_MODEL_CONFIG)
class RoombaPayload:
    """Cuerpo del mensaje MQTT que se publica en cmd/{blid}/delta.

    Los alias no son decorativos: el protocolo de iRobot espera pmap_id y
    user_pmapv_id en snake_case, asi que el nombre del campo (camelCase, como el
    resto del proyecto) y el nombre en el JSON tienen que ir por separado.

    Los campos opcionales son None a proposito: al serializar con
    exclude_none = True desaparecen del JSON, como los
    [JsonIgnore(WhenWritingNull)] de C#. La Roomba rechaza el comando si le
    llegan campos de region vacios.
    """
    command: str | None = None
    time: int = 0
    initiator: str | None = None
    ordered: int | None = None
    pmapId: str | None = Field(default = None, validation_alias = "pmap_id", serialization_alias = "pmap_id")
    userPmapvId: str | None = Field(default = None, validation_alias = "user_pmapv_id", serialization_alias = "user_pmapv_id")
    regions: list[RoombaRegion] | None = None