using HomeService.Application.Interface.Security;
using HomeService.Transversal.Security.Filter;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace HomeService.Transversal.Security.Alexa;

// Filtro que corta la peticion con 401 si la firma de alexa no es valida. Se declara
// igual que ApiKeyAuth: en el controlador (nivel clase) o en la accion (nivel metodo).
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AlexaAuth : Attribute, IAsyncAuthorizationFilter
{
    // Un unico HttpClient para todo el proceso: reutiliza el pool de conexiones.
    private static readonly Lazy<HttpClient> SharedHttpClient = new Lazy<HttpClient>(() => new HttpClient { Timeout = TimeSpan.FromSeconds(10.0) });

    #region GetAlexaRequestVerifier
    public static IAlexaRequestVerifier GetAlexaRequestVerifier() => new AlexaRequestVerifier(SharedHttpClient.Value);
    #endregion

    #region OnAuthorizationAsync
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Bypass para pruebas manuales (X-Debug-Key). Falla cerrado si no esta configurado.
        if (DebugBypass.IsRequested(context.HttpContext.Request.Headers[DebugBypass.HeaderName].ToString()))
            return;

        // Si hay un IAlexaRequestVerifier registrado en el contenedor gana ese, igual
        // que un dependency_override de FastAPI; si no, el de siempre.
        IAlexaRequestVerifier alexaRequestVerifier =
            context.HttpContext.RequestServices.GetService<IAlexaRequestVerifier>() ?? GetAlexaRequestVerifier();

        if (!await alexaRequestVerifier.AmazonApprove(context.HttpContext.Request))
            context.Result = new ObjectResult(new { detail = "Unauthorized" })
            {
                StatusCode = StatusCodes.Status401Unauthorized
            };
    }
    #endregion
}
