using HomeService.Application.DataTransferObject.Alexa;
using HomeService.Application.Interface.Application;
using HomeService.Application.Interface.Security;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.JsonInterchange.Alexa;
using HomeService.Transversal.Security.Filter;
using HomeService.Transversal.Wrapper.Json;
using Microsoft.AspNetCore.Mvc;

namespace HomeService.Service.WebApi.Controllers
{
    // Sin [ApiKeyAuth]: quien llama es Amazon, que no conoce la X-Api-Key. Tampoco
    // [AlexaAuth]: la firma se verifica dentro de la accion porque el bypass de
    // pruebas tiene que saltarse a la vez la firma y la comprobacion del skill id.
    [ApiController]
    [Route("alexa")]
    public class AlexaController : ControllerBase
    {
        private readonly IAlexaApplication _alexaApplication;
        private readonly IAlexaRequestVerifier _alexaRequestVerifier;
        private readonly Settings _settings;

        public AlexaController(IAlexaApplication alexaApplication, IAlexaRequestVerifier alexaRequestVerifier, Settings settings)
        {
            _alexaApplication = alexaApplication;
            _alexaRequestVerifier = alexaRequestVerifier;
            _settings = settings;
        }

        #region SendAlexaOrder
        // El cuerpo no se enlaza como parametro: el verificador tiene que leerlo en
        // crudo antes, y despues AlexaUtils lo vuelve a leer del buffer.
        [HttpPost]
        public async Task<IActionResult> SendAlexaOrder([FromHeader(Name = DebugBypass.HeaderName)] string? xDebugKey)
        {
            AlexaResponseJson alexaResponseJson = new AlexaResponseJson();
            BaseResponseJson baseResponseJson = new BaseResponseJson();

            try
            {
                // Bypass de pruebas (X-Debug-Key): omite firma de Amazon y validacion de skill.
                bool bypass = DebugBypass.IsRequested(xDebugKey);

                if (!bypass && !await _alexaRequestVerifier.AmazonApprove(Request))
                {
                    baseResponseJson.ResponseCodeJson = ResponseCodesJson.UNAUTHORIZED;
                    baseResponseJson.IsSuccess = false;
                    baseResponseJson.Message = "Llamada realizada por un dispositivo no autorizado";
                }
                else
                {
                    AlexaRequestJson? alexaRequestJson = await AlexaUtils.ReadAlexaRequestJson(Request);

                    if (alexaRequestJson?.AlexaRequestData is null)
                    {
                        baseResponseJson.ResponseCodeJson = ResponseCodesJson.BAD_REQUEST;
                        baseResponseJson.IsSuccess = false;
                        baseResponseJson.Message = "AlexaController -> SendAlexaOrder -> AlexaRequestData nula o ausente";
                    }
                    else if (!bypass && !AlexaUtils.CheckSkillOrigin(alexaRequestJson.Session, _settings.AlexaSkillId))
                    {
                        baseResponseJson.ResponseCodeJson = ResponseCodesJson.UNAUTHORIZED;
                        baseResponseJson.IsSuccess = false;
                        baseResponseJson.Message = "Peticion de una skill no autorizada";
                    }
                    else
                    {
                        AlexaRequest alexaRequest = new AlexaRequest
                        {
                            Version = alexaRequestJson.Version,
                            Session = alexaRequestJson.Session,
                            AlexaRequestData = alexaRequestJson.AlexaRequestData,
                            Context = alexaRequestJson.Context,
                        };

                        AlexaResponse alexaResponse = await _alexaApplication.SendAlexaOrder(alexaRequest);

                        baseResponseJson.ResponseCodeJson = ResponseCodesJson.OK;
                        baseResponseJson.IsSuccess = true;
                        baseResponseJson.Message = "All success";

                        alexaResponseJson.Version = alexaResponse.Version;
                        alexaResponseJson.SessionAttributes = alexaResponse.SessionAttributes;
                        alexaResponseJson.AlexaResponseContent = alexaResponse.AlexaResponseContent;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"AlexaController -> SendAlexaOrder -> Error inesperado {ex}");
                baseResponseJson.ResponseCodeJson = ResponseCodesJson.UNEXPECTED_ERROR;
                baseResponseJson.IsSuccess = false;
                baseResponseJson.Message = "Error inesperado al procesar la peticion";
            }

            // Se asigna siempre, tambien en los errores: si no, el codigo HTTP saldria
            // del BaseResponseJson por defecto (UNEXPECTED_ERROR) y un 401 o un 400 se
            // devolverian como 500.
            alexaResponseJson.BaseResponseJson = baseResponseJson;

            return ControllerUtils.HttpResults(alexaResponseJson.BaseResponseJson.ResponseCodeJson, alexaResponseJson);
        }
        #endregion
    }
}
