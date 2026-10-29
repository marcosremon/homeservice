using HomeService.Application.DataTransferObject.Alexa;
using HomeService.Application.Interface.Service;
using HomeService.Domain.Model.Enum.Temperature;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.Wrapper.Alexa.AlexaRequest;
using System.Globalization;

namespace HomeService.Infraestructure.Gateway
{
    public class TemperatureSensorService : ITemperatureSensorService
    {
        private const int _defaultDegrees = 2;

        private readonly IMqttService _mqttService;

        public TemperatureSensorService(IMqttService mqttService)
        {
            _mqttService = mqttService;
        }

        #region ExecuteTemperatureSensorOrder
        public async Task<string> ExecuteTemperatureSensorOrder(string intentName, AlexaRequest alexaRequest)
        {
            string degrees = _getSlot(alexaRequest, "grados");
            string room = _getSlot(alexaRequest, "habitacion");

            switch (intentName)
            {
                case "temperature_sensor_order_subir_temperatura": return await _temperatureManager(TemperatureMode.INCREASE);
                case "temperature_sensor_order_subir_temperatura_en_x_habitacion": return await _temperatureManager(TemperatureMode.INCREASE, room);
                case "temperature_sensor_order_subir_x_temperatura": return await _temperatureManager(TemperatureMode.INCREASE, degrees);
                case "temperature_sensor_order_subir_x_temperatura_en_x_habitacion": return await _temperatureManager(TemperatureMode.INCREASE, degrees, room);

                case "temperature_sensor_order_bajar_temperatura": return await _temperatureManager(TemperatureMode.DECREASE);
                case "temperature_sensor_order_bajar_temperatura_en_x_habitacion": return await _temperatureManager(TemperatureMode.DECREASE, room);
                case "temperature_sensor_order_bajar_x_temperatura": return await _temperatureManager(TemperatureMode.DECREASE, degrees);
                case "temperature_sensor_order_bajar_x_temperatura_en_x_habitacion": return await _temperatureManager(TemperatureMode.DECREASE, degrees, room);

                default: return "Orden no reconocida";
            }
        }
        #endregion

        #region _temperatureManager
        private async Task<string> _temperatureManager(TemperatureMode temperatureMode, params string[] parameters)
        {
            int degrees = _defaultDegrees;
            ThermostatTarget thermostatTarget = ThermostatTarget.FULL_HOUSE;

            foreach (string parameter in parameters)
            {
                if (GeneralUtils.IsNullOrEmpty(parameter))
                    continue;

                // Solo digitos, sin signo ni espacios: si no es un numero es la habitacion.
                if (int.TryParse(parameter, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedDegrees))
                    degrees = parsedDegrees;
                else
                    thermostatTarget = _parseThermostatTarget(parameter);
            }

            bool isSuccess = await _changeThermostatTemperature(temperatureMode, degrees, thermostatTarget);
            if (!isSuccess)
                return "Hubo un error al cambiar la temperatura";

            string action = temperatureMode == TemperatureMode.INCREASE ? "Subiendo" : "Bajando";

            return $"{action} {degrees} grados en {thermostatTarget}";
        }
        #endregion

        #region _changeThermostatTemperature
        private async Task<bool> _changeThermostatTemperature(TemperatureMode temperatureMode, int degrees, ThermostatTarget thermostatTarget)
        {
            string topic = $"home/{thermostatTarget.ToString().ToLowerInvariant()}/thermostat/cmd";

            // El termostato aplica el delta el mismo: aqui no se guarda la consigna actual.
            string sign = temperatureMode == TemperatureMode.INCREASE ? "+" : "-";

            try
            {
                // Sin retain: un delta retenido se reaplicaria en cada reconexion.
                await _mqttService.Publish(topic, $"{sign}{degrees}", retain: false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"TemperatureSensorService -> _changeThermostatTemperature -> {ex.Message}");
                return false;
            }

            return true;
        }
        #endregion

        #region _parseThermostatTarget
        // Las claves van sin tildes y con "bano" en vez de "baño" porque RemoveAccents
        // tambien quita la tilde de la ene. En el python esos casos nunca coincidian.
        private static ThermostatTarget _parseThermostatTarget(string room) =>
            GeneralUtils.RemoveAccents(room.Trim().ToLowerInvariant()) switch
            {
                "cocina" => ThermostatTarget.KITCHEN,
                "cuarto de diego" or "habitacion de diego" or "diego" => ThermostatTarget.DIEGO,
                "cuarto de marcos" or "habitacion de marcos" or "marcos" => ThermostatTarget.MARCOS,
                "cocina y abuela" or "cocina y cuarto de la abuela" or "abuela" => ThermostatTarget.KITCHEN_AND_GRANDMOTHER,
                "cuartos" or "habitaciones" or "dormitorios" => ThermostatTarget.BEDROOMS,
                "cuarto de los padres y bano" or "cuarto de padres y bano" => ThermostatTarget.BEDROOM_AND_TOILET,
                "pasillos y bano" or "pasillo y bano" => ThermostatTarget.HALLWAY_AND_TOILET,
                "salon" or "comedor" => ThermostatTarget.LIVING_ROOM,
                "casa" or "toda la casa" or "casa entera" => ThermostatTarget.FULL_HOUSE,
                _ => ThermostatTarget.FULL_HOUSE,
            };
        #endregion

        #region _getSlot
        private static string _getSlot(AlexaRequest alexaRequest, string slotName)
        {
            Dictionary<string, AlexaSlot>? alexaSlots = alexaRequest.AlexaRequestData.Intent?.Slots;

            if (alexaSlots is null || !alexaSlots.TryGetValue(slotName, out AlexaSlot? alexaSlot) || alexaSlot.Value is null)
                return string.Empty;

            return alexaSlot.Value;
        }
        #endregion
    }
}
