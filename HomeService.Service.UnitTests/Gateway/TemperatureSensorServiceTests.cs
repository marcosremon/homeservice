using HomeService.Application.DataTransferObject.Alexa;
using HomeService.Application.Interface.Service;
using HomeService.Infraestructure.Gateway;
using HomeService.Transversal.Wrapper.Alexa.AlexaRequest;
using Moq;

namespace HomeService.Service.UnitTests.Gateway
{
    public class TemperatureSensorServiceTests
    {
        private readonly Mock<IMqttService> _mqttService = new Mock<IMqttService>();
        private readonly TemperatureSensorService _temperatureSensorService;

        public TemperatureSensorServiceTests()
        {
            _temperatureSensorService = new TemperatureSensorService(_mqttService.Object);
        }

        #region _request
        private static AlexaRequest _request(string? degrees = null, string? room = null)
        {
            Dictionary<string, AlexaSlot> slots = [];

            if (degrees is not null)
                slots["grados"] = new AlexaSlot { Name = "grados", Value = degrees };

            if (room is not null)
                slots["habitacion"] = new AlexaSlot { Name = "habitacion", Value = room };

            return new AlexaRequest { AlexaRequestData = new AlexaRequestData { Intent = new AlexaIntent { Slots = slots } } };
        }
        #endregion

        #region ExecuteTemperatureSensorOrder_PublishesDeltaWithoutRetain
        [Theory]
        [InlineData("temperature_sensor_order_subir_temperatura", null, null, "home/full_house/thermostat/cmd", "+2", "Subiendo 2 grados en FULL_HOUSE")]
        [InlineData("temperature_sensor_order_bajar_temperatura", null, null, "home/full_house/thermostat/cmd", "-2", "Bajando 2 grados en FULL_HOUSE")]
        [InlineData("temperature_sensor_order_subir_x_temperatura", "4", null, "home/full_house/thermostat/cmd", "+4", "Subiendo 4 grados en FULL_HOUSE")]
        [InlineData("temperature_sensor_order_bajar_x_temperatura", "3", null, "home/full_house/thermostat/cmd", "-3", "Bajando 3 grados en FULL_HOUSE")]
        [InlineData("temperature_sensor_order_subir_temperatura_en_x_habitacion", null, "cocina", "home/kitchen/thermostat/cmd", "+2", "Subiendo 2 grados en KITCHEN")]
        [InlineData("temperature_sensor_order_bajar_temperatura_en_x_habitacion", null, "salón", "home/living_room/thermostat/cmd", "-2", "Bajando 2 grados en LIVING_ROOM")]
        [InlineData("temperature_sensor_order_subir_x_temperatura_en_x_habitacion", "5", "cuarto de diego", "home/diego/thermostat/cmd", "+5", "Subiendo 5 grados en DIEGO")]
        [InlineData("temperature_sensor_order_bajar_x_temperatura_en_x_habitacion", "1", "Habitación de Marcos", "home/marcos/thermostat/cmd", "-1", "Bajando 1 grados en MARCOS")]
        public async Task ExecuteTemperatureSensorOrder_PublishesDeltaWithoutRetain(string intentName, string? degrees, string? room, string expectedTopic, string expectedPayload, string expectedMessage)
        {
            string message = await _temperatureSensorService.ExecuteTemperatureSensorOrder(intentName, _request(degrees, room));

            Assert.Equal(expectedMessage, message);
            _mqttService.Verify(service => service.Publish(expectedTopic, expectedPayload, false), Times.Once);
        }
        #endregion

        #region ExecuteTemperatureSensorOrder_RoomNames
        [Theory]
        [InlineData("cocina", "kitchen")]
        [InlineData("  COCINA ", "kitchen")]
        [InlineData("diego", "diego")]
        [InlineData("marcos", "marcos")]
        [InlineData("abuela", "kitchen_and_grandmother")]
        [InlineData("cocina y cuarto de la abuela", "kitchen_and_grandmother")]
        [InlineData("habitaciones", "bedrooms")]
        [InlineData("dormitorios", "bedrooms")]
        [InlineData("cuarto de los padres y baño", "bedroom_and_toilet")]
        [InlineData("pasillos y baño", "hallway_and_toilet")]
        [InlineData("pasillo y bano", "hallway_and_toilet")]
        [InlineData("comedor", "living_room")]
        [InlineData("Salón", "living_room")]
        [InlineData("toda la casa", "full_house")]
        [InlineData("garaje", "full_house")]
        public async Task ExecuteTemperatureSensorOrder_RoomNames(string room, string expectedTopicRoom)
        {
            await _temperatureSensorService.ExecuteTemperatureSensorOrder("temperature_sensor_order_subir_temperatura_en_x_habitacion", _request(room: room));

            _mqttService.Verify(service => service.Publish($"home/{expectedTopicRoom}/thermostat/cmd", "+2", false), Times.Once);
        }
        #endregion

        #region ExecuteTemperatureSensorOrder_NonNumericDegrees_UseDefault
        // Solo digitos: lo que no es un numero se interpreta como habitacion.
        [Theory]
        [InlineData("tres")]
        [InlineData("-3")]
        [InlineData(" 3")]
        [InlineData("")]
        public async Task ExecuteTemperatureSensorOrder_NonNumericDegrees_UseDefault(string degrees)
        {
            await _temperatureSensorService.ExecuteTemperatureSensorOrder("temperature_sensor_order_subir_x_temperatura", _request(degrees));

            _mqttService.Verify(service => service.Publish("home/full_house/thermostat/cmd", "+2", false), Times.Once);
        }
        #endregion

        #region ExecuteTemperatureSensorOrder_UnknownIntent_DoesNotPublish
        [Fact]
        public async Task ExecuteTemperatureSensorOrder_UnknownIntent_DoesNotPublish()
        {
            string message = await _temperatureSensorService.ExecuteTemperatureSensorOrder("temperature_sensor_order_poner_a_20", _request("20"));

            Assert.Equal("Orden no reconocida", message);
            _mqttService.VerifyNoOtherCalls();
        }
        #endregion

        #region ExecuteTemperatureSensorOrder_WithoutSlots_UsesDefaults
        [Fact]
        public async Task ExecuteTemperatureSensorOrder_WithoutSlots_UsesDefaults()
        {
            AlexaRequest alexaRequest = new AlexaRequest { AlexaRequestData = new AlexaRequestData() };

            string message = await _temperatureSensorService.ExecuteTemperatureSensorOrder("temperature_sensor_order_bajar_x_temperatura_en_x_habitacion", alexaRequest);

            Assert.Equal("Bajando 2 grados en FULL_HOUSE", message);
        }
        #endregion

        #region ExecuteTemperatureSensorOrder_PublishFails_ReturnsError
        [Fact]
        public async Task ExecuteTemperatureSensorOrder_PublishFails_ReturnsError()
        {
            _mqttService.Setup(service => service.Publish(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>())).ThrowsAsync(new Exception("broker caido"));

            string message = await _temperatureSensorService.ExecuteTemperatureSensorOrder("temperature_sensor_order_subir_temperatura", _request());

            Assert.Equal("Hubo un error al cambiar la temperatura", message);
        }
        #endregion
    }
}
