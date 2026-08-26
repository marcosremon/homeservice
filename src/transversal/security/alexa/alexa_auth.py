"""
Dependencia de FastAPI que corta la peticion con 401 si la firma de Alexa no es
valida. Se declara igual que get_api_key: en el APIRouter (nivel clase) o en el
decorador del endpoint (nivel metodo).
"""

from functools import lru_cache

import httpx
from fastapi import Depends, Header, HTTPException, Request, status

from application.interface.security.i_alexa_request_verifier import IAlexaRequestVerifier
from transversal.security.filter import debug_bypass
from transversal.security.alexa.alexa_request_verifier import AlexaRequestVerifier

# region get_alexa_request_verifier
@lru_cache
def _get_http_client() -> httpx.AsyncClient:
    return httpx.AsyncClient(timeout=10.0)


def get_alexa_request_verifier() -> IAlexaRequestVerifier:
    return AlexaRequestVerifier(_get_http_client())
# endregion

# region verify_alexa_request
async def verify_alexa_request(
    request: Request,
    x_debug_key: str = Header(default="", alias=debug_bypass.HEADER_NAME),
    alexa_request_verifier: IAlexaRequestVerifier = Depends(get_alexa_request_verifier),
) -> None:
    # Bypass para pruebas manuales (X-Debug-Key). Falla cerrado si no esta configurado.
    if debug_bypass.is_requested(x_debug_key):
        return

    if not await alexa_request_verifier.amazon_approve(request):
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Unauthorized")
# endregion