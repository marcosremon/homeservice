import hmac

from fastapi import Header, HTTPException, status

from transversal.security.filter import DebugBypass
from transversal.common.configuration.Settings import GetSettings

async def GetApiKey(
    xApiKey: str = Header(default="", alias="X-Api-Key"),
    xDebugKey: str = Header(default="", alias=DebugBypass.HEADER_NAME),
) -> None:
    """Exige la cabecera X-Api-Key. Falla cerrado: sin clave configurada, rechaza."""
    # Bypass para pruebas manuales (X-Debug-Key). Falla cerrado si no esta configurado.
    if DebugBypass.IsRequested(xDebugKey):
        return

    expected = GetSettings().internalApiKey

    if not expected:
        raise HTTPException(status_code = status.HTTP_503_SERVICE_UNAVAILABLE, detail="Internal API key no configurada en el servidor.")

    if not hmac.compare_digest(xApiKey.encode("utf-8"), expected.encode("utf-8")):
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Unauthorized")