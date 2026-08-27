"""
Dependencia de FastAPI que corta la peticion con 401 si la firma de Alexa no es
valida. Se declara igual que GetApiKey: en el APIRouter (nivel clase) o en el
decorador del endpoint (nivel metodo).
"""

from functools import lru_cache

import httpx
from fastapi import Depends, Header, HTTPException, Request, status

from application.interface.security.IAlexaRequestVerifier import IAlexaRequestVerifier
from transversal.security.alexa.AlexaRequestVerifier import AlexaRequestVerifier
from transversal.security.filter.DebugBypass import DebugBypass

class AlexaAuth:

    # region _getHttpClient
    @staticmethod
    @lru_cache
    def _getHttpClient() -> httpx.AsyncClient:
        """Un unico AsyncClient para todo el proceso: reutiliza el pool de conexiones."""
        return httpx.AsyncClient(timeout = 10.0)
    # endregion

    # region GetAlexaRequestVerifier
    @staticmethod
    def GetAlexaRequestVerifier() -> IAlexaRequestVerifier:
        return AlexaRequestVerifier(AlexaAuth._getHttpClient())
    # endregion

    # region VerifyAlexaRequest
    @staticmethod
    async def VerifyAlexaRequest(
        request: Request,
        xDebugKey: str = Header(default = "", alias = DebugBypass.HEADER_NAME),
        alexaRequestVerifier: IAlexaRequestVerifier = Depends(GetAlexaRequestVerifier),
    ) -> None:
        # Bypass para pruebas manuales (X-Debug-Key). Falla cerrado si no esta configurado.
        if DebugBypass.IsRequested(xDebugKey):
            return

        if not await alexaRequestVerifier.AmazonApprove(request):
            raise HTTPException(status_code = status.HTTP_401_UNAUTHORIZED, detail = "Unauthorized")
    # endregion