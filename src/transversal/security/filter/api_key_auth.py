import hmac

from fastapi import Header, HTTPException, status

from transversal.security.filter import debug_bypass
from transversal.common.configuration.settings import get_settings

async def get_api_key(
    x_api_key: str = Header(default="", alias="X-Api-Key"),
    x_debug_key: str = Header(default="", alias=debug_bypass.HEADER_NAME),
) -> None:
    """Exige la cabecera X-Api-Key. Falla cerrado: sin clave configurada, rechaza."""
    # Bypass para pruebas manuales (X-Debug-Key). Falla cerrado si no esta configurado.
    if debug_bypass.is_requested(x_debug_key):
        return

    expected = get_settings().internal_api_key

    if not expected:
        raise HTTPException(status_code = status.HTTP_503_SERVICE_UNAVAILABLE, detail="Internal API key no configurada en el servidor.")

    if not hmac.compare_digest(x_api_key.encode("utf-8"), expected.encode("utf-8")):
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Unauthorized")