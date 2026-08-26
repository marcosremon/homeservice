import hmac

from transversal.common.configuration.Settings import GetSettings

HEADER_NAME = "X-Debug-Key"

def IsRequested(xDebugKey: str) -> bool:
    expected = GetSettings().debugBypassKey

    if not expected or not xDebugKey:
        return False

    return hmac.compare_digest(xDebugKey.encode("utf-8"), expected.encode("utf-8"))