import hmac
import os

from fastapi import Header, HTTPException, status


async def get_api_key(x_api_key: str = Header(default="", alias="X-Api-Key")) -> None:
    """Equivalente a [ApiKeyAuth]. Falla cerrado: sin clave configurada, rechaza."""
    expected = os.getenv("INTERNAL_API_KEY", "")

    if not expected:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="Internal API key no configurada en el servidor.",
        )

    if not hmac.compare_digest(x_api_key, expected):
        raise HTTPException(status_code=status.HTTP_401_UNAUTHORIZED, detail="Unauthorized")
