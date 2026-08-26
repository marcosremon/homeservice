from pydantic import Field
from pydantic.dataclasses import dataclass

@dataclass
class RoombaRegionParams:
    no_auto_passes: bool = Field(default = False, alias = "noAutoPasses")
    two_pass: bool = Field(default = False, alias = "twoPass")