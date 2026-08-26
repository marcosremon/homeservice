from pydantic.dataclasses import dataclass

from infraestructure.gateway.roomba.payload.roomba_region import RoombaRegion

@dataclass
class RoombaPayload:
    command: str | None = None
    time: int = 0
    initiator: str | None = None
    ordered: int | None = None
    pmap_id: str | None = None
    user_pmapv_id: str | None = None
    regions: list[RoombaRegion] | None = None