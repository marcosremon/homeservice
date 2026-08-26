"""Equivalente a IOptions<T> / appsettings.json de ASP.NET.

Los valores salen de variables de entorno reales; en local se rellenan desde el
.env de la raiz del proyecto. Si falta algo obligatorio, la app falla al arrancar
en vez de a la primera peticion.
"""

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

    # postgresql+asyncpg://usuario:password@host:puerto/base_de_datos
    database_url: str
    internal_api_key: str
    debug_bypass_key: str = ""

@lru_cache
def get_settings() -> Settings:
    return Settings()