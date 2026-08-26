import hmac

from transversal.common.configuration.settings import get_settings

HEADER_NAME = "X-Debug-Key"

def is_requested(x_debug_key: str) -> bool:
    expected = get_settings().debug_bypass_key

    if not expected or not x_debug_key:
        return False

    return hmac.compare_digest(x_debug_key.encode("utf-8"), expected.encode("utf-8"))