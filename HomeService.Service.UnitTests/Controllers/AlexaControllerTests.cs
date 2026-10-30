using System.Text;
using HomeService.Application.DataTransferObject.Alexa;
using HomeService.Application.Interface.Application;
using HomeService.Application.Interface.Security;
using HomeService.Service.UnitTests.TestSetup;
using HomeService.Service.WebApi.Controllers;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.JsonInterchange.Alexa;
using HomeService.Transversal.Wrapper.Alexa.AlexaResponse;
using HomeService.Transversal.Wrapper.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace HomeService.Service.UnitTests.Controllers
{
    public class AlexaControllerTests
    {
        private const string _launchBody = "{\"version\":\"1.0\",\"session\":{\"application\":{\"applicationId\":\"" + TestEnvironment.AlexaSkillId + "\"}},\"request\":{\"type\":\"LaunchRequest\",\"requestId\":\"r1\",\"timestamp\":\"2026-10-30T10:00:00Z\"}}";

        private readonly Mock<IAlexaApplication> _alexaApplication = new Mock<IAlexaApplication>();
        private readonly Mock<IAlexaRequestVerifier> _alexaRequestVerifier = new Mock<IAlexaRequestVerifier>();
        private readonly Settings _settings = new Settings { AlexaSkillId = TestEnvironment.AlexaSkillId };

        #region _createController
        private AlexaController _createController(string body)
        {
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
            httpContext.Request.ContentType = "application/json";

            return new AlexaController(_alexaApplication.Object, _alexaRequestVerifier.Object, _settings)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
            };
        }
        #endregion

        #region _setupSuccessfulApplication
        private void _setupSuccessfulApplication()
        {
            _alexaApplication.Setup(application => application.SendAlexaOrder(It.IsAny<AlexaRequest>()))
                .ReturnsAsync(new AlexaResponse
                {
                    Version = "1.0",
                    SessionAttributes = new Dictionary<string, string> { ["history"] = "[]" },
                    AlexaResponseContent = new AlexaResponseContent
                    {
                        OutputSpeech = new AlexaOutputSpeech { Type = "PlainText", Text = "Hola" },
                        ShouldEndSession = false,
                    },
                });
        }
        #endregion

        #region SendAlexaOrder_SignatureRejected_Returns401WithoutCallingApplication
        [Fact]
        public async Task SendAlexaOrder_SignatureRejected_Returns401WithoutCallingApplication()
        {
            _alexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(false);

            IActionResult actionResult = await _createController(_launchBody).SendAlexaOrder(null);

            AlexaResponseJson responseJson = ResultAssert.Json<AlexaResponseJson>(actionResult, 401);
            Assert.Equal(ResponseCodesJson.UNAUTHORIZED, responseJson.BaseResponseJson.ResponseCodeJson);
            Assert.Equal("Llamada realizada por un dispositivo no autorizado", responseJson.BaseResponseJson.Message);
            _alexaApplication.Verify(application => application.SendAlexaOrder(It.IsAny<AlexaRequest>()), Times.Never);
        }
        #endregion

        #region SendAlexaOrder_ValidSignatureAndSkill_Returns200WithApplicationResponse
        [Fact]
        public async Task SendAlexaOrder_ValidSignatureAndSkill_Returns200WithApplicationResponse()
        {
            _alexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(true);
            _setupSuccessfulApplication();

            IActionResult actionResult = await _createController(_launchBody).SendAlexaOrder(null);

            AlexaResponseJson responseJson = ResultAssert.Json<AlexaResponseJson>(actionResult, 200);
            Assert.Equal("1.0", responseJson.Version);
            Assert.Equal("Hola", responseJson.AlexaResponseContent.OutputSpeech.Text);
            Assert.False(responseJson.AlexaResponseContent.ShouldEndSession);
            Assert.Equal("[]", responseJson.SessionAttributes!["history"]);
            Assert.True(responseJson.BaseResponseJson.IsSuccess);
        }
        #endregion

        #region SendAlexaOrder_ValidRequest_MapsBodyToApplicationRequest
        [Fact]
        public async Task SendAlexaOrder_ValidRequest_MapsBodyToApplicationRequest()
        {
            AlexaRequest? captured = null;
            _alexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(true);
            _alexaApplication.Setup(application => application.SendAlexaOrder(It.IsAny<AlexaRequest>()))
                .Callback<AlexaRequest>(request => captured = request)
                .ReturnsAsync(new AlexaResponse());

            string body = "{\"version\":\"1.0\",\"session\":{\"sessionId\":\"s1\",\"application\":{\"applicationId\":\"" + TestEnvironment.AlexaSkillId + "\"},\"attributes\":{\"history\":\"[]\"}}," +
                          "\"request\":{\"type\":\"IntentRequest\",\"requestId\":\"r2\",\"timestamp\":\"t\",\"intent\":{\"name\":\"light_order_encender_luces_salon\",\"slots\":{\"query\":{\"name\":\"query\",\"value\":\"hola\"}}}}," +
                          "\"context\":{\"System\":{\"application\":{\"applicationId\":\"app\"},\"user\":{\"userId\":\"u1\"}}}}";

            await _createController(body).SendAlexaOrder(null);

            Assert.NotNull(captured);
            Assert.Equal("1.0", captured.Version);
            Assert.Equal("s1", captured.Session!.SessionId);
            Assert.Equal("[]", captured.Session.Attributes!["history"]);
            Assert.Equal("IntentRequest", captured.AlexaRequestData.Type);
            Assert.Equal("light_order_encender_luces_salon", captured.AlexaRequestData.Intent!.Name);
            Assert.Equal("hola", captured.AlexaRequestData.Intent.Slots!["query"].Value);
            Assert.Equal("u1", captured.Context!.System.User.UserId);
        }
        #endregion

        #region SendAlexaOrder_WrongSkillId_Returns401
        [Fact]
        public async Task SendAlexaOrder_WrongSkillId_Returns401()
        {
            _alexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(true);
            string body = _launchBody.Replace(TestEnvironment.AlexaSkillId, "amzn1.ask.skill.otra");

            IActionResult actionResult = await _createController(body).SendAlexaOrder(null);

            AlexaResponseJson responseJson = ResultAssert.Json<AlexaResponseJson>(actionResult, 401);
            Assert.Equal("Peticion de una skill no autorizada", responseJson.BaseResponseJson.Message);
            _alexaApplication.Verify(application => application.SendAlexaOrder(It.IsAny<AlexaRequest>()), Times.Never);
        }
        #endregion

        #region SendAlexaOrder_NoSession_Returns401
        [Fact]
        public async Task SendAlexaOrder_NoSession_Returns401()
        {
            _alexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(true);

            IActionResult actionResult = await _createController("{\"request\":{\"type\":\"LaunchRequest\"}}").SendAlexaOrder(null);

            ResultAssert.Json<AlexaResponseJson>(actionResult, 401);
        }
        #endregion

        #region SendAlexaOrder_BodyWithoutRequest_Returns400
        [Theory]
        [InlineData("{\"version\":\"1.0\"}")]
        [InlineData("esto no es json")]
        [InlineData("")]
        public async Task SendAlexaOrder_BodyWithoutRequest_Returns400(string body)
        {
            _alexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(true);

            IActionResult actionResult = await _createController(body).SendAlexaOrder(null);

            AlexaResponseJson responseJson = ResultAssert.Json<AlexaResponseJson>(actionResult, 400);
            Assert.Equal(ResponseCodesJson.BAD_REQUEST, responseJson.BaseResponseJson.ResponseCodeJson);
        }
        #endregion

        #region SendAlexaOrder_DebugBypass_SkipsSignatureAndSkillCheck
        [Fact]
        public async Task SendAlexaOrder_DebugBypass_SkipsSignatureAndSkillCheck()
        {
            _setupSuccessfulApplication();
            string body = _launchBody.Replace(TestEnvironment.AlexaSkillId, "amzn1.ask.skill.otra");

            IActionResult actionResult = await _createController(body).SendAlexaOrder(TestEnvironment.DebugBypassKey);

            ResultAssert.Json<AlexaResponseJson>(actionResult, 200);
            _alexaRequestVerifier.Verify(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>()), Times.Never);
        }
        #endregion

        #region SendAlexaOrder_WrongDebugKey_StillVerifiesSignature
        [Fact]
        public async Task SendAlexaOrder_WrongDebugKey_StillVerifiesSignature()
        {
            _alexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(false);

            IActionResult actionResult = await _createController(_launchBody).SendAlexaOrder("clave-mala");

            ResultAssert.Json<AlexaResponseJson>(actionResult, 401);
            _alexaRequestVerifier.Verify(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>()), Times.Once);
        }
        #endregion

        #region SendAlexaOrder_BodyAlreadyReadByVerifier_IsReadAgain
        // El verificador lee el cuerpo en crudo; el controlador tiene que poder
        // releerlo desde el principio.
        [Fact]
        public async Task SendAlexaOrder_BodyAlreadyReadByVerifier_IsReadAgain()
        {
            _alexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>()))
                .Returns<HttpRequest>(async request =>
                {
                    using StreamReader reader = new StreamReader(request.Body, leaveOpen: true);
                    await reader.ReadToEndAsync();
                    return true;
                });
            _setupSuccessfulApplication();

            IActionResult actionResult = await _createController(_launchBody).SendAlexaOrder(null);

            ResultAssert.Json<AlexaResponseJson>(actionResult, 200);
        }
        #endregion

        #region SendAlexaOrder_ApplicationThrows_Returns500WithGenericMessage
        [Fact]
        public async Task SendAlexaOrder_ApplicationThrows_Returns500WithGenericMessage()
        {
            _alexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(true);
            _alexaApplication.Setup(application => application.SendAlexaOrder(It.IsAny<AlexaRequest>())).ThrowsAsync(new Exception("secreto interno"));

            IActionResult actionResult = await _createController(_launchBody).SendAlexaOrder(null);

            AlexaResponseJson responseJson = ResultAssert.Json<AlexaResponseJson>(actionResult, 500);
            Assert.Equal(ResponseCodesJson.UNEXPECTED_ERROR, responseJson.BaseResponseJson.ResponseCodeJson);
            Assert.Equal("Error inesperado al procesar la peticion", responseJson.BaseResponseJson.Message);
        }
        #endregion

        #region SendAlexaOrder_VerifierThrows_Returns500
        [Fact]
        public async Task SendAlexaOrder_VerifierThrows_Returns500()
        {
            _alexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ThrowsAsync(new Exception("cert"));

            IActionResult actionResult = await _createController(_launchBody).SendAlexaOrder(null);

            ResultAssert.Json<AlexaResponseJson>(actionResult, 500);
        }
        #endregion
    }
}
