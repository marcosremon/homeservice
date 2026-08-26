from enum import Enum
from typing import TypeVar

TEnum = TypeVar("TEnum", bound = Enum)

class GeneralUtils:
    @staticmethod
    def is_null_or_empty(value: str) -> bool:
        return value is None or value == ""

    @staticmethod
    def is_null_or_white_space(value: str) -> bool:
        return value is None or value.strip() == ""

    @staticmethod
    def parse_enum(enum_type: type[TEnum], value: str, default: TEnum) -> TEnum:
        try:
            return enum_type[value.strip().upper()]
        except (KeyError, AttributeError):
            return default

    @staticmethod
    def parse_enum_exact(enum_type: type[TEnum], value: str | None) -> TEnum | None:
        """Equivalente a Enum.TryParse SIN ignoreCase.

        Hace falta para los enums cuyos nombres vienen de fuera tal cual
        (IntentName, AlexaRequestType): ahi "ConversationIntent" tiene que
        coincidir letra a letra, asi que parse_enum no vale porque mayusculiza.
        """
        try:
            return enum_type[value] if value is not None else None
        except KeyError:
            return None