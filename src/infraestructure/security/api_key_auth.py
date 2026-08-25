import hmac

from fastapi import Header, HTTPException, status

from transversal.common.configuration.settings import get_settings


async def get_api_key(x_api_key: str = Header(default="", alias="X-Api-Key")) -> None:
    """Equivalente a [ApiKeyAuth]. Falla cerrado: sin clave configurada, rechaza."""
    expected = get_settings().internal_api_key

    if not expected:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="Internal API key no configurada en el servidor.",
        )

    if not hmac.compare_digest(x_api_key, expected):
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Unauthorized")
