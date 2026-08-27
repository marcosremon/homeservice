import hmac

from transversal.common.configuration.Settings import GetSettings

class DebugBypass:
    HEADER_NAME: str = "X-Debug-Key"

    #region IsRequested
    @staticmethod
    def IsRequested(xDebugKey: str) -> bool:
        expected: str = GetSettings().debugBypassKey

        if not expected or not xDebugKey:
            return False

        return hmac.compare_digest(xDebugKey.encode("utf-8"), expected.encode("utf-8"))
    #endregion