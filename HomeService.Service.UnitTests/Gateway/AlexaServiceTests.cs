using HomeService.Application.DataTransferObject.Alexa;
using HomeService.Application.DataTransferObject.Gemini;
using HomeService.Application.Interface.Service;
using HomeService.Infraestructure.Gateway;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Wrapper.Alexa.AlexaRequest;
using Moq;

namespace HomeService.Service.UnitTests.Gateway
{
    public class AlexaServiceTests
    {
        private readonly Mock<ILightService> _lightService = new Mock<ILightService>();
        private readonly Mock<IRoombaService> _roombaService = new Mock<IRoombaService>();
        private readonly Mock<IGeminiService> _geminiService = new Mock<IGeminiService>();
        private readonly Mock<IComputerStatusService> _computerStatusService = new Mock<IComputerStatusService>();
        private readonly Mock<ITemperatureSensorService> _temperatureSensorService = new Mock<ITemperatureSensorService>();
        private readonly AlexaService _alexaService;

        public AlexaServiceTests()
        {
            _alexaService = new AlexaService(
                _lightService.Object,
                _roombaService.Object,
                _geminiService.Object,
                _computerStatusService.Object,
                _temperatureSensorService.Object,
                new Settings { AlexaVersion = "1.0" });
        }

        #region _intentRequest
        private static AlexaRequest _intentRequest(string intentName, Dictionary<string, AlexaSlot>? slots = null, Dictionary<string, string>? attributes = null) => new AlexaRequest
        {
            Session = new AlexaSession { Attributes = attributes },
            AlexaRequestData = new AlexaRequestData
            {
                Type = "IntentRequest",
                Intent = new AlexaIntent { Name = intentName, Slots = slots },
            },
        };
        #endregion

        #region _querySlot
        private static Dictionary<string, AlexaSlot> _querySlot(string? value) => new Dictionary<string, AlexaSlot>
        {
            ["query"] = new AlexaSlot { Name = "query", Value = value },
        };
        #endregion

        #region SendAlexaOrder_LaunchRequest_GreetsAndKeepsSessionOpen
        [Fact]
        public async Task SendAlexaOrder_LaunchRequest_GreetsAndKeepsSessionOpen()
        {
            AlexaRequest alexaRequest = new AlexaRequest { AlexaRequestData = new AlexaRequestData { Type = "LaunchRequest" } };

            AlexaResponse alexaResponse = await _alexaService.SendAlexaOrder(alexaRequest);

            Assert.Equal("1.0", alexaResponse.Version);
            Assert.StartsWith("Hola", alexaResponse.AlexaResponseContent.OutputSpeech.Text);
            Assert.Equal("PlainText", alexaResponse.AlexaResponseContent.OutputSpeech.Type);
            Assert.False(alexaResponse.AlexaResponseContent.ShouldEndSession);
            Assert.NotNull(alexaResponse.AlexaResponseContent.Reprompt);
        }
        #endregion

        #region SendAlexaOrder_LaunchRequestIsCaseSensitive
        [Fact]
        public async Task SendAlexaOrder_LaunchRequestIsCaseSensitive()
        {
            AlexaRequest alexaRequest = new AlexaRequest { AlexaRequestData = new AlexaRequestData { Type = "launchrequest" } };

            AlexaResponse alexaResponse = await _alexaService.SendAlexaOrder(alexaRequest);

            Assert.Equal("No te he entendido, ¿puedes repetir?", alexaResponse.AlexaResponseContent.OutputSpeech.Text);
        }
        #endregion

        #region SendAlexaOrder_DomainIntent_RoutesByPrefixWithRawName
        [Theory]
        [InlineData("light_order_encender_luces_salon")]
        [InlineData("light_order_")]
        public async Task SendAlexaOrder_LightIntent_RoutesToLightService(string intentName)
        {
            _lightService.Setup(service => service.ExecuteLightOrder(intentName)).ReturnsAsync("Encendiendo la luz de LIVING_ROOM");

            AlexaResponse alexaResponse = await _alexaService.SendAlexaOrder(_intentRequest(intentName));

            Assert.Equal("Encendiendo la luz de LIVING_ROOM", alexaResponse.AlexaResponseContent.OutputSpeech.Text);
            Assert.Equal("SSML", alexaResponse.AlexaResponseContent.OutputSpeech.Type);
            Assert.True(alexaResponse.AlexaResponseContent.ShouldEndSession);
            Assert.Equal("1.0", alexaResponse.Version);
            _lightService.Verify(service => service.ExecuteLightOrder(intentName), Times.Once);
        }
        #endregion

        #region SendAlexaOrder_RoombaIntent_RoutesToRoombaServiceWithRequest
        [Fact]
        public async Task SendAlexaOrder_RoombaIntent_RoutesToRoombaServiceWithRequest()
        {
            AlexaRequest alexaRequest = _intentRequest("roomba_order_limpiar_cocina");
            _roombaService.Setup(service => service.ExecuteRoombaOrder("roomba_order_limpiar_cocina", alexaRequest)).ReturnsAsync("Iniciando limpieza de kitchen.");

            AlexaResponse alexaResponse = await _alexaService.SendAlexaOrder(alexaRequest);

            Assert.Equal("Iniciando limpieza de kitchen.", alexaResponse.AlexaResponseContent.OutputSpeech.Text);
            _lightService.VerifyNoOtherCalls();
            _computerStatusService.VerifyNoOtherCalls();
        }
        #endregion

        #region SendAlexaOrder_ComputerIntent_RoutesToComputerStatusService
        [Fact]
        public async Task SendAlexaOrder_ComputerIntent_RoutesToComputerStatusService()
        {
            _computerStatusService.Setup(service => service.ExecuteComputerStatusOrder("computer_status_order_get_status")).ReturnsAsync("El ordenador está encendido.");

            AlexaResponse alexaResponse = await _alexaService.SendAlexaOrder(_intentRequest("computer_status_order_get_status"));

            Assert.Equal("El ordenador está encendido.", alexaResponse.AlexaResponseContent.OutputSpeech.Text);
        }
        #endregion

        #region SendAlexaOrder_TemperatureIntent_RoutesToTemperatureSensorService
        [Fact]
        public async Task SendAlexaOrder_TemperatureIntent_RoutesToTemperatureSensorService()
        {
            AlexaRequest alexaRequest = _intentRequest("temperature_sensor_order_subir_temperatura");
            _temperatureSensorService.Setup(service => service.ExecuteTemperatureSensorOrder("temperature_sensor_order_subir_temperatura", alexaRequest)).ReturnsAsync("Subiendo 2 grados en FULL_HOUSE");

            AlexaResponse alexaResponse = await _alexaService.SendAlexaOrder(alexaRequest);

            Assert.Equal("Subiendo 2 grados en FULL_HOUSE", alexaResponse.AlexaResponseContent.OutputSpeech.Text);
        }
        #endregion

        #region SendAlexaOrder_ServiceReturnsEmpty_UsesDefaultSpeech
        // computer_status_order_encender_ordenador con el ordenador apagado devuelve "".
        [Fact]
        public async Task SendAlexaOrder_ServiceReturnsEmpty_UsesDefaultSpeech()
        {
            _computerStatusService.Setup(service => service.ExecuteComputerStatusOrder(It.IsAny<string>())).ReturnsAsync(string.Empty);

            AlexaResponse alexaResponse = await _alexaService.SendAlexaOrder(_intentRequest("computer_status_order_encender_ordenador"));

            Assert.Equal("Comando recibido correctamente en el Home Lab.", alexaResponse.AlexaResponseContent.OutputSpeech.Text);
        }
        #endregion

        #region SendAlexaOrder_UnknownIntent_AsksToRepeat
        [Theory]
        [InlineData("AMAZON.HelpIntent")]
        [InlineData("Light_order_encender")]
        [InlineData("lightorder")]
        [InlineData("")]
        public async Task SendAlexaOrder_UnknownIntent_AsksToRepeat(string intentName)
        {
            AlexaResponse alexaResponse = await _alexaService.SendAlexaOrder(_intentRequest(intentName));

            Assert.Equal("No te he entendido, ¿puedes repetir?", alexaResponse.AlexaResponseContent.OutputSpeech.Text);
            Assert.False(alexaResponse.AlexaResponseContent.ShouldEndSession);
            _lightService.VerifyNoOtherCalls();
            _roombaService.VerifyNoOtherCalls();
            _computerStatusService.VerifyNoOtherCalls();
            _temperatureSensorService.VerifyNoOtherCalls();
            _geminiService.VerifyNoOtherCalls();
        }
        #endregion

        #region SendAlexaOrder_NoIntent_AsksToRepeat
        [Fact]
        public async Task SendAlexaOrder_NoIntent_AsksToRepeat()
        {
            AlexaRequest alexaRequest = new AlexaRequest { AlexaRequestData = new AlexaRequestData { Type = "IntentRequest" } };

            AlexaResponse alexaResponse = await _alexaService.SendAlexaOrder(alexaRequest);

            Assert.Equal("No te he entendido, ¿puedes repetir?", alexaResponse.AlexaResponseContent.OutputSpeech.Text);
        }
        #endregion

        #region SendAlexaOrder_ConversationWithoutQuery_AsksToRepeatWithoutCallingGemini
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task SendAlexaOrder_ConversationWithoutQuery_AsksToRepeatWithoutCallingGemini(string? query)
        {
            AlexaResponse alexaResponse = await _alexaService.SendAlexaOrder(_intentRequest("ConversationIntent", _querySlot(query)));

            Assert.Equal("No te he entendido, ¿puedes repetir?", alexaResponse.AlexaResponseContent.OutputSpeech.Text);
            Assert.False(alexaResponse.AlexaResponseContent.ShouldEndSession);
            _geminiService.VerifyNoOtherCalls();
        }
        #endregion

        #region SendAlexaOrder_ConversationWithoutSlots_AsksToRepeat
        [Fact]
        public async Task SendAlexaOrder_ConversationWithoutSlots_AsksToRepeat()
        {
            AlexaResponse alexaResponse = await _alexaService.SendAlexaOrder(_intentRequest("ConversationIntent"));

            Assert.Equal("No te he entendido, ¿puedes repetir?", alexaResponse.AlexaResponseContent.OutputSpeech.Text);
        }
        #endregion

        #region SendAlexaOrder_Conversation_SendsHistoryAndStoresUpdatedOne
        [Fact]
        public async Task SendAlexaOrder_Conversation_SendsHistoryAndStoresUpdatedOne()
        {
            GeminiTurnRequest? captured = null;
            _geminiService.Setup(service => service.Converse(It.IsAny<GeminiTurnRequest>()))
                .Callback<GeminiTurnRequest>(request => captured = request)
                .ReturnsAsync(new GeminiTurnResponse
                {
                    Replay = "Madrid",
                    UpdatedHistory =
                    [
                        new GeminiTurn { Role = "user", Text = "hola" },
                        new GeminiTurn { Role = "model", Text = "buenas" },
                        new GeminiTurn { Role = "user", Text = "capital de españa" },
                        new GeminiTurn { Role = "model", Text = "Madrid" },
                    ],
                });

            Dictionary<string, string> attributes = new Dictionary<string, string>
            {
                ["history"] = "[{\"role\":\"user\",\"text\":\"hola\"},{\"role\":\"model\",\"text\":\"buenas\"}]",
            };

            AlexaResponse alexaResponse = await _alexaService.SendAlexaOrder(_intentRequest("ConversationIntent", _querySlot("capital de españa"), attributes));

            Assert.NotNull(captured);
            Assert.Equal("capital de españa", captured.UserText);
            Assert.Equal(2, captured.History.Count);
            Assert.Equal("user", captured.History[0].Role);
            Assert.Equal("buenas", captured.History[1].Text);

            Assert.Equal("Madrid", alexaResponse.AlexaResponseContent.OutputSpeech.Text);
            Assert.False(alexaResponse.AlexaResponseContent.ShouldEndSession);
            Assert.NotNull(alexaResponse.AlexaResponseContent.Reprompt);

            string history = alexaResponse.SessionAttributes!["history"];
            Assert.Contains("\"role\":\"model\"", history);
            Assert.Contains("\"text\":\"Madrid\"", history);
        }
        #endregion

        #region SendAlexaOrder_ConversationWithCorruptHistory_StartsFresh
        [Theory]
        [InlineData("no es json")]
        [InlineData("")]
        [InlineData("null")]
        public async Task SendAlexaOrder_ConversationWithCorruptHistory_StartsFresh(string history)
        {
            GeminiTurnRequest? captured = null;
            _geminiService.Setup(service => service.Converse(It.IsAny<GeminiTurnRequest>()))
                .Callback<GeminiTurnRequest>(request => captured = request)
                .ReturnsAsync(new GeminiTurnResponse { Replay = "ok" });

            await _alexaService.SendAlexaOrder(_intentRequest("ConversationIntent", _querySlot("hola"), new Dictionary<string, string> { ["history"] = history }));

            Assert.NotNull(captured);
            Assert.Empty(captured.History);
        }
        #endregion

        #region SendAlexaOrder_ServiceThrows_DoesNotThrow
        [Fact]
        public async Task SendAlexaOrder_ServiceThrows_DoesNotThrow()
        {
            _lightService.Setup(service => service.ExecuteLightOrder(It.IsAny<string>())).ThrowsAsync(new Exception("mqtt"));

            AlexaResponse alexaResponse = await _alexaService.SendAlexaOrder(_intentRequest("light_order_encender_luces_salon"));

            Assert.NotNull(alexaResponse);
        }
        #endregion
    }
}
