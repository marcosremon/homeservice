import hmac
from fastapi import Header, HTTPException, status
from transversal.security.filter.DebugBypass import DebugBypass
from transversal.common.configuration.Settings import GetSettings

class ApiKeyAuth:

    _API_KEY: str = Header(default = "", alias = "X-Api-Key")
    _DEBUG_KEY: str = Header(default = "", alias = DebugBypass.HEADER_NAME)

    #region GetApiKey
    @staticmethod
    async def GetApiKey(xApiKey: str = _API_KEY, xDebugKey: str = _DEBUG_KEY) -> None:
        """Exige la cabecera X-Api-Key. Falla cerrado: sin clave configurada, rechaza."""
        # Bypass para pruebas manuales (X-Debug-Key). Falla cerrado si no esta configurado.
        if DebugBypass.IsRequested(xDebugKey):
            return

        expected = GetSettings().internalApiKey

        if not expected:
            raise HTTPException(status_code = status.HTTP_503_SERVICE_UNAVAILABLE, detail = "Internal API key no configurada en el servidor.")

        if not hmac.compare_digest(xApiKey.encode("utf-8"), expected.encode("utf-8")):
            raise HTTPException(status_code = status.HTTP_401_UNAUTHORIZED, detail = "Unauthorized")
    #endregion