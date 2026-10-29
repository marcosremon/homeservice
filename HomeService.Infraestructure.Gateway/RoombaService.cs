using HomeService.Application.DataTransferObject.Alexa;
using HomeService.Application.Interface.Service;
using HomeService.Domain.Model.Enum.Roomba;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.Wrapper.Alexa.AlexaRequest;

namespace HomeService.Infraestructure.Gateway
{
    // Singleton: los temporizadores de "limpia en X tiempo" tienen que sobrevivir a
    // la peticion que los programo.
    public class RoombaService : IRoombaService
    {
        #region ExecuteRoombaOrder
        public async Task<string> ExecuteRoombaOrder(string intentName, AlexaRequest alexaRequest)
        {
            switch (intentName)
            {
                case "roomba_order_limpiar_cocina": return await RoombaUtils.StartRoomba(RoombaTarget.KITCHEN);
                case "roomba_order_limpiar_cuarto_diego": return await RoombaUtils.StartRoomba(RoombaTarget.DIEGO);
                case "roomba_order_limpiar_cuarto_marcos": return await RoombaUtils.StartRoomba(RoombaTarget.MARCOS);
                case "roomba_order_limpiar_cuarto_abuela_y_cocina": return await RoombaUtils.StartRoomba(RoombaTarget.KITCHEN_AND_GRANDMOTHER);
                case "roomba_order_limpiar_cuarto_diego_y_cuarto_marcos": return await RoombaUtils.StartRoomba(RoombaTarget.BEDROOMS);
                case "roomba_order_limpiar_banyos_y_cuarto_padres": return await RoombaUtils.StartRoomba(RoombaTarget.BEDROOM_AND_TOILET);
                case "roomba_order_limpiar_pasillos_y_banyo": return await RoombaUtils.StartRoomba(RoombaTarget.HALLWAY_AND_TOILET);
                case "roomba_order_limpiar_comedor": return await RoombaUtils.StartRoomba(RoombaTarget.LIVING_ROOM);
                case "roomba_order_limpiar_casa_completa": return await RoombaUtils.StartRoomba(RoombaTarget.FULL_HOUSE);

                case "roomba_order_limpiar_casa_en_x_tiempo": return _startRoombaWithTimerBackground(alexaRequest, RoombaTarget.FULL_HOUSE);
                case "roomba_order_limpiar_cocina_en_x_tiempo": return _startRoombaWithTimerBackground(alexaRequest, RoombaTarget.KITCHEN);
                case "roomba_order_limpiar_comedor_en_x_tiempo": return _startRoombaWithTimerBackground(alexaRequest, RoombaTarget.LIVING_ROOM);
                case "roomba_order_limpiar_cuarto_diego_en_x_tiempo": return _startRoombaWithTimerBackground(alexaRequest, RoombaTarget.DIEGO);
                case "roomba_order_limpiar_cuarto_marcos_en_x_tiempo": return _startRoombaWithTimerBackground(alexaRequest, RoombaTarget.MARCOS);
                case "roomba_order_limpiar_cuarto_abuela_y_cocina_en_x_tiempo": return _startRoombaWithTimerBackground(alexaRequest, RoombaTarget.KITCHEN_AND_GRANDMOTHER);
                case "roomba_order_limpiar_cuarto_diego_y_cuarto_marcos_en_x_tiempo": return _startRoombaWithTimerBackground(alexaRequest, RoombaTarget.BEDROOMS);
                case "roomba_order_limpiar_banyos_y_cuarto_padres_en_x_tiempo": return _startRoombaWithTimerBackground(alexaRequest, RoombaTarget.BEDROOM_AND_TOILET);
                case "roomba_order_limpiar_pasillos_y_banyo_en_x_tiempo": return _startRoombaWithTimerBackground(alexaRequest, RoombaTarget.HALLWAY_AND_TOILET);

                case "roomba_order_pausar_roomba": return await RoombaUtils.PauseRoomba();
                case "roomba_order_enviar_roomba_a_casa": return await RoombaUtils.SendRoombaHome();

                default: return "Orden no reconocida";
            }
        }
        #endregion

        #region _startRoombaWithTimerBackground
        private static string _startRoombaWithTimerBackground(AlexaRequest alexaRequest, RoombaTarget roombaTarget)
        {
            Dictionary<string, AlexaSlot>? alexaSlots = alexaRequest.AlexaRequestData.Intent?.Slots;
            if (alexaSlots is null || alexaSlots.Count == 0)
                return "No se encontro el tiempo indicado.";

            // alexa manda la duracion en uno de estos tres slots segun el intent.
            string time = _getSlotValue(alexaSlots, "duracion")
                ?? _getSlotValue(alexaSlots, "tiempo")
                ?? _getSlotValue(alexaSlots, "time")
                ?? string.Empty;

            if (GeneralUtils.IsNullOrEmpty(time))
                return "No se encontro el tiempo indicado.";

            // Sin await a proposito: la respuesta a alexa sale ya y la limpieza
            // arranca cuando venza el plazo.
            _ = Task.Run(() => _startRoombaAfter(time, roombaTarget));

            return $"De acuerdo, limpiare en {time}.";
        }
        #endregion

        #region _startRoombaAfter
        private static async Task _startRoombaAfter(string time, RoombaTarget roombaTarget)
        {
            try
            {
                TimeSpan duration = AlexaUtils.ParseAlexaDuration(time);

                await Task.Delay(duration);
                await RoombaUtils.StartRoomba(roombaTarget);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Timer] Error en background: {ex.Message}");
            }
        }
        #endregion

        #region _getSlotValue
        private static string? _getSlotValue(Dictionary<string, AlexaSlot> alexaSlots, string slotName) =>
            alexaSlots.TryGetValue(slotName, out AlexaSlot? alexaSlot) ? alexaSlot.Value : null;
        #endregion
    }
}
