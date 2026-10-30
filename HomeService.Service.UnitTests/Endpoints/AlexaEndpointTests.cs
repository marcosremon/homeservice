using System.Net;
using System.Text.Json;
using HomeService.Application.DataTransferObject.Alexa;
using HomeService.Service.UnitTests.TestSetup;
using HomeService.Transversal.Wrapper.Alexa.AlexaResponse;
using Microsoft.AspNetCore.Http;
using Moq;

namespace HomeService.Service.UnitTests.Endpoints
{
    public class AlexaEndpointTests : EndpointTestBase
    {
        private const string _url = "/api/alexa";
        private const string _launchBody = "{\"version\":\"1.0\",\"session\":{\"application\":{\"applicationId\":\"" + TestEnvironment.AlexaSkillId + "\"}},\"request\":{\"type\":\"LaunchRequest\",\"requestId\":\"r1\",\"timestamp\":\"2026-10-30T10:00:00Z\"}}";

        public AlexaEndpointTests(HomeServiceWebApplicationFactory factory) : base(factory) { }

        #region _setupApplication
        private void _setupApplication(Dictionary<string, string>? sessionAttributes = null)
        {
            Factory.AlexaApplication.Setup(application => application.SendAlexaOrder(It.IsAny<AlexaRequest>()))
                .ReturnsAsync(new AlexaResponse
                {
                    Version = "1.0",
                    SessionAttributes = sessionAttributes,
                    AlexaResponseContent = new AlexaResponseContent
                    {
                        OutputSpeech = new AlexaOutputSpeech { Type = "PlainText", Text = "Hola" },
                        Reprompt = new AlexaReprompt { OutputSpeech = new AlexaOutputSpeech { Type = "PlainText", Text = "Sigues ahi?" } },
                        ShouldEndSession = false,
                    },
                });
        }
        #endregion

        #region SendAlexaOrder_DoesNotRequireApiKey
        // Amazon no conoce la X-Api-Key: el endpoint de Alexa no puede pedirla.
        [Fact]
        public async Task SendAlexaOrder_DoesNotRequireApiKey()
        {
            Factory.AlexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(true);
            _setupApplication();

            HttpResponseMessage response = await Send(HttpMethod.Post, _url, _launchBody, apiKey: null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        #endregion

        #region SendAlexaOrder_SerializesAmazonContract
        [Fact]
        public async Task SendAlexaOrder_SerializesAmazonContract()
        {
            Factory.AlexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(true);
            _setupApplication(new Dictionary<string, string> { ["history"] = "[]" });

            HttpResponseMessage response = await Send(HttpMethod.Post, _url, _launchBody, apiKey: null);

            JsonElement json = await ReadJson(response);
            Assert.Equal("1.0", json.GetProperty("version").GetString());
            Assert.Equal("[]", json.GetProperty("sessionAttributes").GetProperty("history").GetString());

            JsonElement content = json.GetProperty("response");
            Assert.Equal("PlainText", content.GetProperty("outputSpeech").GetProperty("type").GetString());
            Assert.Equal("Hola", content.GetProperty("outputSpeech").GetProperty("text").GetString());
            Assert.Equal("Sigues ahi?", content.GetProperty("reprompt").GetProperty("outputSpeech").GetProperty("text").GetString());
            Assert.False(content.GetProperty("shouldEndSession").GetBoolean());

            // El BaseResponseJson es control interno: no viaja a Amazon.
            Assert.False(json.TryGetProperty("BaseResponseJson", out _));
            Assert.False(json.TryGetProperty("baseResponseJson", out _));
            Assert.False(json.TryGetProperty("responseCodeJson", out _));
        }
        #endregion

        #region SendAlexaOrder_NullSessionAttributes_AreOmitted
        [Fact]
        public async Task SendAlexaOrder_NullSessionAttributes_AreOmitted()
        {
            Factory.AlexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(true);
            _setupApplication();

            HttpResponseMessage response = await Send(HttpMethod.Post, _url, _launchBody, apiKey: null);

            JsonElement json = await ReadJson(response);
            Assert.False(json.TryGetProperty("sessionAttributes", out _));
        }
        #endregion

        #region SendAlexaOrder_InvalidSignature_Returns401
        [Fact]
        public async Task SendAlexaOrder_InvalidSignature_Returns401()
        {
            Factory.AlexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(false);

            HttpResponseMessage response = await Send(HttpMethod.Post, _url, _launchBody, apiKey: null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Factory.AlexaApplication.Verify(application => application.SendAlexaOrder(It.IsAny<AlexaRequest>()), Times.Never);
        }
        #endregion

        #region SendAlexaOrder_VerifierAndControllerReadSameBody
        // El verificador consume el cuerpo y el controlador lo vuelve a leer: solo
        // funciona porque Program.cs activa EnableBuffering en el pipeline.
        [Fact]
        public async Task SendAlexaOrder_VerifierAndControllerReadSameBody()
        {
            string? bodySeenByVerifier = null;
            Factory.AlexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>()))
                .Returns<HttpRequest>(async request =>
                {
                    request.Body.Position = 0;
                    using StreamReader reader = new StreamReader(request.Body, leaveOpen: true);
                    bodySeenByVerifier = await reader.ReadToEndAsync();
                    return true;
                });

            AlexaRequest? captured = null;
            Factory.AlexaApplication.Setup(application => application.SendAlexaOrder(It.IsAny<AlexaRequest>()))
                .Callback<AlexaRequest>(request => captured = request)
                .ReturnsAsync(new AlexaResponse());

            HttpResponseMessage response = await Send(HttpMethod.Post, _url, _launchBody, apiKey: null);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(_launchBody, bodySeenByVerifier);
            Assert.Equal("LaunchRequest", captured!.AlexaRequestData.Type);
        }
        #endregion

        #region SendAlexaOrder_DebugBypass_SkipsVerifier
        [Fact]
        public async Task SendAlexaOrder_DebugBypass_SkipsVerifier()
        {
            _setupApplication();
            string body = _launchBody.Replace(TestEnvironment.AlexaSkillId, "otra-skill");

            HttpResponseMessage response = await Send(HttpMethod.Post, _url, body, apiKey: null, debugKey: TestEnvironment.DebugBypassKey);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Factory.AlexaRequestVerifier.Verify(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>()), Times.Never);
        }
        #endregion

        #region SendAlexaOrder_WrongSkill_Returns401
        [Fact]
        public async Task SendAlexaOrder_WrongSkill_Returns401()
        {
            Factory.AlexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(true);
            string body = _launchBody.Replace(TestEnvironment.AlexaSkillId, "otra-skill");

            HttpResponseMessage response = await Send(HttpMethod.Post, _url, body, apiKey: null);

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        #endregion

        #region SendAlexaOrder_BodyWithoutRequest_Returns400
        [Fact]
        public async Task SendAlexaOrder_BodyWithoutRequest_Returns400()
        {
            Factory.AlexaRequestVerifier.Setup(verifier => verifier.AmazonApprove(It.IsAny<HttpRequest>())).ReturnsAsync(true);

            HttpResponseMessage response = await Send(HttpMethod.Post, _url, "{\"version\":\"1.0\"}", apiKey: null);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
        #endregion
    }
}
