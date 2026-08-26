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