using HomeService.Application.DataTransferObject.Alexa;
using HomeService.Infraestructure.Gateway;
using HomeService.Transversal.Wrapper.Alexa.AlexaRequest;

namespace HomeService.Service.UnitTests.Gateway
{
    // Solo los caminos que no llegan a RoombaUtils: el resto abre MQTT contra la roomba.
    public class RoombaServiceTests
    {
        private readonly RoombaService _roombaService = new RoombaService();

        #region _request
        private static AlexaRequest _request(Dictionary<string, AlexaSlot>? slots) =>
            new AlexaRequest { AlexaRequestData = new AlexaRequestData { Intent = new AlexaIntent { Slots = slots } } };
        #endregion

        #region ExecuteRoombaOrder_UnknownOrder
        [Theory]
        [InlineData("roomba_order_limpiar_garaje")]
        [InlineData("roomba_order_")]
        [InlineData("")]
        public async Task ExecuteRoombaOrder_UnknownOrder(string intentName)
        {
            Assert.Equal("Orden no reconocida", await _roombaService.ExecuteRoombaOrder(intentName, _request(null)));
        }
        #endregion

        #region ExecuteRoombaOrder_TimerWithoutTime_SaysSoAndDoesNotSchedule
        [Theory]
        [InlineData("roomba_order_limpiar_casa_en_x_tiempo")]
        [InlineData("roomba_order_limpiar_cocina_en_x_tiempo")]
        [InlineData("roomba_order_limpiar_comedor_en_x_tiempo")]
        [InlineData("roomba_order_limpiar_cuarto_diego_en_x_tiempo")]
        [InlineData("roomba_order_limpiar_cuarto_marcos_en_x_tiempo")]
        [InlineData("roomba_order_limpiar_cuarto_abuela_y_cocina_en_x_tiempo")]
        [InlineData("roomba_order_limpiar_cuarto_diego_y_cuarto_marcos_en_x_tiempo")]
        [InlineData("roomba_order_limpiar_banyos_y_cuarto_padres_en_x_tiempo")]
        [InlineData("roomba_order_limpiar_pasillos_y_banyo_en_x_tiempo")]
        public async Task ExecuteRoombaOrder_TimerWithoutSlots_SaysSo(string intentName)
        {
            Assert.Equal("No se encontro el tiempo indicado.", await _roombaService.ExecuteRoombaOrder(intentName, _request(null)));
            Assert.Equal("No se encontro el tiempo indicado.", await _roombaService.ExecuteRoombaOrder(intentName, _request([])));
        }
        #endregion

        #region ExecuteRoombaOrder_TimerWithEmptyOrUnknownSlot_SaysSo
        [Theory]
        [InlineData("duracion", null)]
        [InlineData("duracion", "")]
        [InlineData("otro_slot", "PT10M")]
        public async Task ExecuteRoombaOrder_TimerWithEmptyOrUnknownSlot_SaysSo(string slotName, string? value)
        {
            Dictionary<string, AlexaSlot> slots = new Dictionary<string, AlexaSlot> { [slotName] = new AlexaSlot { Name = slotName, Value = value } };

            Assert.Equal("No se encontro el tiempo indicado.", await _roombaService.ExecuteRoombaOrder("roomba_order_limpiar_casa_en_x_tiempo", _request(slots)));
        }
        #endregion
    }
}
