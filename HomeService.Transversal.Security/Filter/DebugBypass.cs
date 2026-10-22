using System.Security.Cryptography;
using System.Text;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Common.Utils;

namespace HomeService.Transversal.Security.Filter;

public static class DebugBypass
{
    public const string HeaderName = "X-Debug-Key";

    #region IsRequested
    public static bool IsRequested(string? xDebugKey)
    {
        string expected = Settings.GetSettings().DebugBypassKey;

        if (GeneralUtils.IsNullOrEmpty(expected) || GeneralUtils.IsNullOrEmpty(xDebugKey))
            return false;

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(xDebugKey!),
            Encoding.UTF8.GetBytes(expected));
    }
    #endregion
}
