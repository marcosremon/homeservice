from functools import lru_cache
from pathlib import Path

from pydantic_settings import BaseSettings, SettingsConfigDict

_PROJECT_ROOT = Path(__file__).resolve().parents[4]

class Settings(BaseSettings):
    model_config = SettingsConfigDict(
        env_file = _PROJECT_ROOT / ".env",
        env_file_encoding = "utf-8",
        extra = "ignore",
    )

    # Equivalente al bloque Kestrel de appsettings.json.
    app_host: str = "0.0.0.0"
    app_port: int = 8000

    # postgresql+asyncpg://usuario:password@host:puerto/base_de_datos
    database_url: str
    internal_api_key: str
    debug_bypass_key: str = ""

    # Equivalente a AlexaSettings de appsettings.json.
    alexa_version: str = "1.0"
    alexa_skill_id: str = ""

    # Equivalente a GeminiSettings de appsettings.json.
    gemini_api_key: str = ""
    gemini_model: str = ""

    # Equivalente a MqttSettings de appsettings.json.
    mqtt_host: str = ""
    mqtt_port: int = 1883
    mqtt_user: str = ""
    mqtt_password: str = ""

    # Equivalente a IRobotSettings de appsettings.json.
    roomba_id: str = ""
    roomba_port: str = "8883"
    roomba_blid: str = ""
    roomba_passwd: str = ""
    roomba_pmap_id: str = ""
    roomba_pmap_version: str = ""

    # Equivalente a LanParametersSettings de appsettings.json. Los nombres llevan
    # el prefijo lan_ porque las variables del .env se llaman LAN_*.
    lan_computer_ip: str = ""
    lan_computer_mac: str = ""
    lan_broadcast_ip: str = ""
    lan_cachyos_user: str = ""

@lru_cache
def get_settings() -> Settings:
    return Settings()