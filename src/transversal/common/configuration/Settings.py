import re
from functools import lru_cache
from pathlib import Path

from pydantic_settings import BaseSettings, SettingsConfigDict

_PROJECT_ROOT: Path = Path(__file__).resolve().parents[4]

def _EnvironmentVariableName(fieldName: str) -> str:
    return re.sub(r"(?<!^)(?=[A-Z])", "_", fieldName).upper()

class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file = _PROJECT_ROOT / ".env",
        env_file_encoding = "utf-8",
        extra = "ignore",
        alias_generator = _EnvironmentVariableName,
        populate_by_name = True,
    )

    #region properties

    appHost: str = "0.0.0.0"
    appPort: int = 8000

    # postgresql+asyncpg://usuario:password@host:puerto/base_de_datos
    databaseUrl: str
    internalApiKey: str
    debugBypassKey: str = ""

    alexaVersion: str = "1.0"
    alexaSkillId: str = ""

    geminiApiKey: str = ""
    geminiModel: str = ""

    mqttHost: str = ""
    mqttPort: int = 1883
    mqttUser: str = ""
    mqttPassword: str = ""

    roombaId: str = ""
    roombaPort: str = "8883"
    roombaBlid: str = ""
    roombaPasswd: str = ""
    roombaPmapId: str = ""
    roombaPmapVersion: str = ""

    # cualquiera que lo conozca puede publicar y leer, por eso no tiene default.
    ntfyBaseUrl: str = "https://ntfy.sh"
    ntfyTopic: str = ""

    # el prefijo lan_ porque las variables del .env se llaman LAN_*.
    lanComputerIp: str = ""
    lanComputerMac: str = ""
    lanBroadcastIp: str = ""
    lanCachyosUser: str = ""

    #endregion

@lru_cache
def GetSettings() -> Settings:
    return Settings()