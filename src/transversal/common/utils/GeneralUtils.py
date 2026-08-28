import unicodedata
from enum import Enum
from typing import TypeVar

TEnum = TypeVar("TEnum", bound = Enum)

class GeneralUtils:

    #region IsNullOrEmpty
    @staticmethod
    def IsNullOrEmpty(value: str) -> bool:
        return value is None or value == ""
    #endregion

    #region IsNullOrWhiteSpace
    @staticmethod
    def IsNullOrWhiteSpace(value: str) -> bool:
        return value is None or value.strip() == ""
    #endregion

    #region RemoveAccents
    @staticmethod
    def RemoveAccents(value: str) -> str:
        """Quita tildes y dieresis para comparar lo que manda Alexa.

        Alexa no es consistente con los acentos en los valores de slot, asi
        que "salón" y "salon" tienen que caer en la misma rama del match.
        """
        if GeneralUtils.IsNullOrEmpty(value):
            return ""

        decomposed: str = unicodedata.normalize("NFD", value)

        return "".join(char for char in decomposed if unicodedata.category(char) != "Mn")
    #endregion

    #region ParseEnum
    @staticmethod
    def ParseEnum(enumType: type[TEnum], value: str, default: TEnum) -> TEnum:
        try:
            return enumType[value.strip().upper()]
        except Exception:
            return default
    #endregion

    #region ParseEnumExact
    @staticmethod
    def ParseEnumExact(enumType: type[TEnum], value: str | None) -> TEnum | None:
        """Equivalente a Enum.TryParse SIN ignoreCase.

        Hace falta para los enums cuyos nombres vienen de fuera tal cual
        (IntentName, AlexaRequestType): ahi "ConversationIntent" tiene que
        coincidir letra a letra, asi que parse_enum no vale porque mayusculiza.
        """
        try:
            return enumType[value] if value is not None else None
        except Exception:
            return None
    #endregion