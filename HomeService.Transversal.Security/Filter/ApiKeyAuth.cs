using System.Security.Cryptography;
using System.Text;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Common.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HomeService.Transversal.Security.Filter;

// Equivalente al Depends(GetApiKey) de FastAPI: se declara en el controlador (nivel
// clase) o en la accion (nivel metodo).
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiKeyAuth : Attribute, IAuthorizationFilter
{
    private const string ApiKeyHeaderName = "X-Api-Key";

    #region OnAuthorization
    // Exige la cabecera X-Api-Key. Falla cerrado: sin clave configurada, rechaza.
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Bypass para pruebas manuales (X-Debug-Key). Falla cerrado si no esta configurado.
        if (DebugBypass.IsRequested(context.HttpContext.Request.Headers[DebugBypass.HeaderName].ToString()))
            return;

        string expected = Settings.GetSettings().InternalApiKey;

        if (GeneralUtils.IsNullOrEmpty(expected))
        {
            context.Result = new ObjectResult(new { detail = "Internal API key no configurada en el servidor." })
            {
                StatusCode = StatusCodes.Status503ServiceUnavailable
            };

            return;
        }

        string xApiKey = context.HttpContext.Request.Headers[ApiKeyHeaderName].ToString();

        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(xApiKey), Encoding.UTF8.GetBytes(expected)))
            context.Result = new ObjectResult(new { detail = "Unauthorized" })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
    }
    #endregion
}
