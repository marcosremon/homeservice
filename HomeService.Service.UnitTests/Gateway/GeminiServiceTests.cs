using System.Net;
using System.Text.Json;
using HomeService.Application.DataTransferObject.Gemini;
using HomeService.Infraestructure.Gateway;
using HomeService.Service.UnitTests.TestSetup;
using HomeService.Transversal.Common.Configuration;

namespace HomeService.Service.UnitTests.Gateway
{
    public class GeminiServiceTests
    {
        #region _create
        private static GeminiService _create(FakeHttpMessageHandler handler) =>
            new GeminiService(new FakeHttpClientFactory(handler), new Settings { GeminiApiKey = "clave-gemini", GeminiModel = "gemini-2.5-flash" });
        #endregion

        #region _candidate
        private static string _candidate(string text, string finishReason = "STOP") =>
            JsonSerializer.Serialize(new { candidates = new[] { new { finishReason, content = new { parts = new[] { new { text } } } } } });
        #endregion

        #region Converse_Success_ReturnsReplyAndAppendsTurns
        [Fact]
        public async Task Converse_Success_ReturnsReplyAndAppendsTurns()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, _candidate("  Madrid es la capital.  "));

            GeminiTurnResponse response = await _create(handler).Converse(new GeminiTurnRequest { UserText = "capital de españa" });

            Assert.Equal("Madrid es la capital.", response.Replay);
            Assert.Equal(2, response.UpdatedHistory.Count);
            Assert.Equal("user", response.UpdatedHistory[0].Role);
            Assert.Equal("capital de españa", response.UpdatedHistory[0].Text);
            Assert.Equal("model", response.UpdatedHistory[1].Role);
            Assert.Equal("Madrid es la capital.", response.UpdatedHistory[1].Text);
        }
        #endregion

        #region Converse_SendsKeyInHeaderAndModelInUrl
        // La clave va en cabecera, nunca en la query.
        [Fact]
        public async Task Converse_SendsKeyInHeaderAndModelInUrl()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, _candidate("ok"));

            await _create(handler).Converse(new GeminiTurnRequest { UserText = "hola" });

            HttpRequestMessage request = Assert.Single(handler.Requests);
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent", request.RequestUri!.ToString());
            Assert.Equal("clave-gemini", request.Headers.GetValues("x-goog-api-key").Single());
            Assert.DoesNotContain("clave-gemini", request.RequestUri.Query);
        }
        #endregion

        #region Converse_PayloadIsCamelCaseWithHistoryAndConfig
        [Fact]
        public async Task Converse_PayloadIsCamelCaseWithHistoryAndConfig()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, _candidate("ok"));
            GeminiTurnRequest request = new GeminiTurnRequest
            {
                UserText = "y de francia?",
                History =
                [
                    new GeminiTurn { Role = "user", Text = "capital de españa" },
                    new GeminiTurn { Role = "model", Text = "Madrid" },
                ],
            };

            await _create(handler).Converse(request);

            using JsonDocument payload = JsonDocument.Parse(handler.RequestBodies.Single());
            JsonElement root = payload.RootElement;

            Assert.False(string.IsNullOrEmpty(root.GetProperty("systemInstruction").GetProperty("parts")[0].GetProperty("text").GetString()));

            JsonElement contents = root.GetProperty("contents");
            Assert.Equal(3, contents.GetArrayLength());
            Assert.Equal("user", contents[0].GetProperty("role").GetString());
            Assert.Equal("Madrid", contents[1].GetProperty("parts")[0].GetProperty("text").GetString());
            Assert.Equal("y de francia?", contents[2].GetProperty("parts")[0].GetProperty("text").GetString());

            JsonElement generationConfig = root.GetProperty("generationConfig");
            Assert.Equal(1024, generationConfig.GetProperty("maxOutputTokens").GetInt32());
            Assert.Equal(0, generationConfig.GetProperty("thinkingConfig").GetProperty("thinkingBudget").GetInt32());
        }
        #endregion

        #region Converse_HttpError_ReturnsApologyAndKeepsHistoryConsistent
        [Theory]
        [InlineData(HttpStatusCode.TooManyRequests)]
        [InlineData(HttpStatusCode.InternalServerError)]
        [InlineData(HttpStatusCode.Unauthorized)]
        public async Task Converse_HttpError_ReturnsApologyAndKeepsHistoryConsistent(HttpStatusCode statusCode)
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(statusCode, "{\"error\":{}}");

            GeminiTurnResponse response = await _create(handler).Converse(new GeminiTurnRequest { UserText = "hola" });

            Assert.Equal("Lo siento, no he podido pensar la respuesta ahora mismo.", response.Replay);
            Assert.Equal("model", response.UpdatedHistory.Last().Role);
        }
        #endregion

        #region Converse_Timeout_AsksToRepeat
        [Fact]
        public async Task Converse_Timeout_AsksToRepeat()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Throwing(new TaskCanceledException());

            GeminiTurnResponse response = await _create(handler).Converse(new GeminiTurnRequest { UserText = "hola" });

            Assert.Equal("Me ha llevado demasiado, ¿puedes repetirlo?", response.Replay);
        }
        #endregion

        #region Converse_NetworkError_ReturnsGenericProblem
        [Fact]
        public async Task Converse_NetworkError_ReturnsGenericProblem()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Throwing(new HttpRequestException("dns"));

            GeminiTurnResponse response = await _create(handler).Converse(new GeminiTurnRequest { UserText = "hola" });

            Assert.Equal("Ha habido un problema al responder.", response.Replay);
        }
        #endregion

        #region Converse_ResponseWithoutText_ReturnsNoAnswer
        [Theory]
        [InlineData("{}")]
        [InlineData("{\"candidates\":[]}")]
        [InlineData("{\"candidates\":[{\"finishReason\":\"SAFETY\"}]}")]
        [InlineData("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"   \"}]}}]}")]
        public async Task Converse_ResponseWithoutText_ReturnsNoAnswer(string body)
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, body);

            GeminiTurnResponse response = await _create(handler).Converse(new GeminiTurnRequest { UserText = "hola" });

            Assert.Equal("No tengo una respuesta para eso.", response.Replay);
        }
        #endregion

        #region Converse_JoinsAllParts
        [Fact]
        public async Task Converse_JoinsAllParts()
        {
            string body = "{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Hola, \"},{\"text\":\"que tal\"}]}}]}";
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, body);

            GeminiTurnResponse response = await _create(handler).Converse(new GeminiTurnRequest { UserText = "hola" });

            Assert.Equal("Hola, que tal", response.Replay);
        }
        #endregion

        #region Converse_LongReply_IsTruncatedForAlexa
        // Alexa rechaza un outputSpeech de mas de 8000 caracteres.
        [Fact]
        public async Task Converse_LongReply_IsTruncatedForAlexa()
        {
            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, _candidate(new string('a', 9000)));

            GeminiTurnResponse response = await _create(handler).Converse(new GeminiTurnRequest { UserText = "hola" });

            Assert.Equal(7900, response.Replay.Length);
        }
        #endregion

        #region Converse_LongHistory_IsTrimmedAndStartsWithUser
        [Fact]
        public async Task Converse_LongHistory_IsTrimmedAndStartsWithUser()
        {
            List<GeminiTurn> history = [];
            for (int turn = 0; turn < 10; turn++)
            {
                history.Add(new GeminiTurn { Role = "user", Text = new string('u', 1000) });
                history.Add(new GeminiTurn { Role = "model", Text = new string('m', 1000) });
            }

            FakeHttpMessageHandler handler = FakeHttpMessageHandler.Returning(HttpStatusCode.OK, _candidate("ok"));

            GeminiTurnResponse response = await _create(handler).Converse(new GeminiTurnRequest { UserText = "ultima", History = history });

            Assert.True(response.UpdatedHistory.Sum(turn => turn.Text.Length) <= 10000);
            Assert.Equal("user", response.UpdatedHistory.First().Role);
            Assert.Equal("ultima", response.UpdatedHistory[^2].Text);
            Assert.Equal("ok", response.UpdatedHistory[^1].Text);
        }
        #endregion
    }
}
