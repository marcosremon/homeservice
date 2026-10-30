using System.Text;
using HomeService.Domain.Model.Enum.Alexa;
using HomeService.Service.UnitTests.TestSetup;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.JsonInterchange.Alexa;
using HomeService.Transversal.Wrapper.Alexa.AlexaRequest;
using HomeService.Transversal.Wrapper.Alexa.AlexaResponse;
using Microsoft.AspNetCore.Http;

namespace HomeService.Service.UnitTests.Utils
{
    public class AlexaUtilsTests
    {
        #region ParseAlexaDuration_ValidDurations
        [Theory]
        [InlineData("PT10M", 600)]
        [InlineData("PT45S", 45)]
        [InlineData("PT1H", 3600)]
        [InlineData("PT1H30M", 5400)]
        [InlineData("PT1H30M15S", 5415)]
        [InlineData("P1D", 86400)]
        [InlineData("P1DT2H", 93600)]
        [InlineData("PT0S", 0)]
        public void ParseAlexaDuration_ValidDurations(string isoDuration, int expectedSeconds)
        {
            Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), AlexaUtils.ParseAlexaDuration(isoDuration));
        }
        #endregion

        #region ParseAlexaDuration_MonthsAreReadAsMinutes
        // Limitacion conocida y documentada: la M de meses se lee como minutos.
        [Fact]
        public void ParseAlexaDuration_MonthsAreReadAsMinutes()
        {
            Assert.Equal(TimeSpan.FromMinutes(2), AlexaUtils.ParseAlexaDuration("P2M"));
        }
        #endregion

        #region ParseAlexaDuration_InvalidFormat_ReturnsZero
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("10M")]
        [InlineData("diez minutos")]
        [InlineData("p10m")]
        public void ParseAlexaDuration_InvalidFormat_ReturnsZero(string? isoDuration)
        {
            Assert.Equal(TimeSpan.Zero, AlexaUtils.ParseAlexaDuration(isoDuration!));
        }
        #endregion

        #region ParseAlexaOrder
        [Theory]
        [InlineData(IntentName.roomba_order_, "roomba_order_")]
        [InlineData(IntentName.computer_status_order_, "computer_status_order_")]
        [InlineData(IntentName.light_order_, "light_order_")]
        [InlineData(IntentName.ConversationIntent, "")]
        public void ParseAlexaOrder(IntentName intentName, string expected)
        {
            Assert.Equal(expected, AlexaUtils.ParseAlexaOrder(intentName));
        }
        #endregion

        #region BuildAlexaResponse_WithMessage_SpeaksItAndEndsSession
        [Fact]
        public void BuildAlexaResponse_WithMessage_SpeaksItAndEndsSession()
        {
            AlexaResponseContent content = AlexaUtils.BuildAlexaResponse("light_order_x", "Encendiendo la luz");

            Assert.Equal("SSML", content.OutputSpeech.Type);
            Assert.Equal("Encendiendo la luz", content.OutputSpeech.Text);
            Assert.True(content.ShouldEndSession);
            Assert.Null(content.Reprompt);
        }
        #endregion

        #region BuildAlexaResponse_EmptyMessage_UsesDefaultText
        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void BuildAlexaResponse_EmptyMessage_UsesDefaultText(string? message)
        {
            AlexaResponseContent content = AlexaUtils.BuildAlexaResponse("x", message!);

            Assert.Equal("Comando recibido correctamente en el Home Lab.", content.OutputSpeech.Text);
        }
        #endregion

        #region BuildConversationResponse_KeepOpen_AddsReprompt
        // Sin reprompt alexa cierra el microfono aunque ShouldEndSession sea false.
        [Fact]
        public void BuildConversationResponse_KeepOpen_AddsReprompt()
        {
            AlexaResponseContent content = AlexaUtils.BuildConversationResponse("Hola", keepSessionOpen: true);

            Assert.Equal("PlainText", content.OutputSpeech.Type);
            Assert.Equal("Hola", content.OutputSpeech.Text);
            Assert.False(content.ShouldEndSession);
            Assert.NotNull(content.Reprompt);
            Assert.Equal("PlainText", content.Reprompt.OutputSpeech.Type);
            Assert.False(string.IsNullOrEmpty(content.Reprompt.OutputSpeech.Text));
        }
        #endregion

        #region BuildConversationResponse_Close_HasNoReprompt
        [Fact]
        public void BuildConversationResponse_Close_HasNoReprompt()
        {
            AlexaResponseContent content = AlexaUtils.BuildConversationResponse("Adios", keepSessionOpen: false);

            Assert.True(content.ShouldEndSession);
            Assert.Null(content.Reprompt);
        }
        #endregion

        #region BuildConversationResponse_EmptySpeech_UsesDefault
        [Fact]
        public void BuildConversationResponse_EmptySpeech_UsesDefault()
        {
            Assert.Equal("Si?", AlexaUtils.BuildConversationResponse(string.Empty, true).OutputSpeech.Text);
        }
        #endregion

        #region CheckSkillOrigin
        [Theory]
        [InlineData(TestEnvironment.AlexaSkillId, TestEnvironment.AlexaSkillId, true)]
        [InlineData("otra-skill", TestEnvironment.AlexaSkillId, false)]
        [InlineData("", TestEnvironment.AlexaSkillId, false)]
        [InlineData(TestEnvironment.AlexaSkillId, "", false)]
        [InlineData("", "", false)]
        public void CheckSkillOrigin(string applicationId, string skillId, bool expected)
        {
            AlexaSession session = new AlexaSession { Application = new HomeService.Transversal.Wrapper.Alexa.AlexaRequest.AlexaApplication { ApplicationId = applicationId } };

            Assert.Equal(expected, AlexaUtils.CheckSkillOrigin(session, skillId));
        }
        #endregion

        #region CheckSkillOrigin_NullSession_ReturnsFalse
        [Fact]
        public void CheckSkillOrigin_NullSession_ReturnsFalse()
        {
            Assert.False(AlexaUtils.CheckSkillOrigin(null, TestEnvironment.AlexaSkillId));
        }
        #endregion

        #region ReadAlexaRequestJson_ValidBody_DeserializesAmazonKeys
        [Fact]
        public async Task ReadAlexaRequestJson_ValidBody_DeserializesAmazonKeys()
        {
            string body = "{\"version\":\"1.0\",\"session\":{\"new\":true,\"sessionId\":\"s\",\"application\":{\"applicationId\":\"app\"},\"user\":{\"userId\":\"u\"}}," +
                          "\"request\":{\"type\":\"IntentRequest\",\"requestId\":\"r\",\"timestamp\":\"t\",\"intent\":{\"name\":\"roomba_order_limpiar_cocina\",\"confirmationStatus\":\"NONE\",\"slots\":{\"tiempo\":{\"name\":\"tiempo\",\"value\":\"PT10M\"}}}}," +
                          "\"context\":{\"System\":{\"application\":{\"applicationId\":\"app\"},\"user\":{\"userId\":\"u\"}}}}";

            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

            AlexaRequestJson? alexaRequestJson = await AlexaUtils.ReadAlexaRequestJson(httpContext.Request);

            Assert.NotNull(alexaRequestJson);
            Assert.True(alexaRequestJson.Session!.New);
            Assert.Equal("app", alexaRequestJson.Session.Application.ApplicationId);
            Assert.Equal("u", alexaRequestJson.Session.User.UserId);
            Assert.Equal("IntentRequest", alexaRequestJson.AlexaRequestData!.Type);
            Assert.Equal("roomba_order_limpiar_cocina", alexaRequestJson.AlexaRequestData.Intent!.Name);
            Assert.Equal("PT10M", alexaRequestJson.AlexaRequestData.Intent.Slots!["tiempo"].Value);
            Assert.Equal("app", alexaRequestJson.Context!.System.Application.ApplicationId);
        }
        #endregion

        #region ReadAlexaRequestJson_RewindsAlreadyReadBody
        [Fact]
        public async Task ReadAlexaRequestJson_RewindsAlreadyReadBody()
        {
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{\"request\":{\"type\":\"LaunchRequest\"}}"));
            httpContext.Request.Body.Seek(0, SeekOrigin.End);

            AlexaRequestJson? alexaRequestJson = await AlexaUtils.ReadAlexaRequestJson(httpContext.Request);

            Assert.Equal("LaunchRequest", alexaRequestJson!.AlexaRequestData!.Type);
        }
        #endregion

        #region ReadAlexaRequestJson_InvalidBody_ReturnsNull
        [Theory]
        [InlineData("")]
        [InlineData("no es json")]
        [InlineData("[1,2,3]")]
        public async Task ReadAlexaRequestJson_InvalidBody_ReturnsNull(string body)
        {
            DefaultHttpContext httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));

            Assert.Null(await AlexaUtils.ReadAlexaRequestJson(httpContext.Request));
        }
        #endregion
    }
}
