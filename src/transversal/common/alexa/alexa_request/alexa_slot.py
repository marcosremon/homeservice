from pydantic.dataclasses import dataclass

@dataclass
class AlexaSlot:
    name: str = ""
    value: str | None = None