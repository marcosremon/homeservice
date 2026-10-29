using HomeService.Application.DataTransferObject.Light;
using HomeService.Application.DataTransferObject.Light.GetLightByLocation;
using HomeService.Application.DataTransferObject.Light.PatchLightStatus;
using HomeService.Application.Interface.Repository;
using HomeService.Application.Interface.Service;
using HomeService.Domain.Model.Enum.Light;
using HomeService.Transversal.Common.Utils;

namespace HomeService.Infraestructure.Gateway
{
    public class LightService : ILightService
    {
        private readonly IMqttService _mqttService;
        private readonly ILightRepository _lightRepository;

        public LightService(IMqttService mqttService, ILightRepository lightRepository)
        {
            _mqttService = mqttService;
            _lightRepository = lightRepository;
        }

        #region ExecuteLightOrder
        public async Task<string> ExecuteLightOrder(string intentName)
        {
            switch (intentName)
            {
                case "light_order_encender_luces_salon": return await _lightManager(LightLocation.LIVING_ROOM, LightStatus.ON);
                case "light_order_apagar_luces_salon": return await _lightManager(LightLocation.LIVING_ROOM, LightStatus.OFF);

                default: return "Orden no reconocida";
            }
        }
        #endregion

        #region _lightManager
        private async Task<string> _lightManager(LightLocation lightLocation, LightStatus lightStatus)
        {
            LightDto? lightDto = await _getLight(lightLocation);
            if (lightDto is null)
                return $"No encuentro la luz de {lightLocation}";

            if (lightStatus == LightStatus.ON && lightDto.IsOn)
                return $"La luz de {lightLocation} ya esta encendida";

            if (lightStatus == LightStatus.OFF && !lightDto.IsOn)
                return $"La luz de {lightLocation} ya esta apagada";

            await _changeLightStatus(lightDto, lightStatus);

            return lightStatus == LightStatus.ON
                ? $"Encendiendo la luz de {lightLocation}"
                : $"Apagando la luz de {lightLocation}";
        }
        #endregion

        #region _changeLightStatus
        private async Task _changeLightStatus(LightDto lightDto, LightStatus lightStatus)
        {
            string topic = !GeneralUtils.IsNullOrEmpty(lightDto.MqttTopic)
                ? lightDto.MqttTopic
                : $"home/{lightDto.Location.ToString().ToLowerInvariant()}/lights/cmd";

            await _mqttService.Publish(topic, lightStatus.ToString());

            // Se guarda el estado nuevo. En el python se mandaba lightDto.isOn, el de
            // antes del cambio, asi que la luz cambiaba pero la base de datos no.
            PatchLightStatusRequest patchLightStatusRequest = new PatchLightStatusRequest
            {
                LightLocation = lightDto.Location,
                IsOn = lightStatus == LightStatus.ON,
            };

            await _lightRepository.PatchLightStatus(patchLightStatusRequest);
        }
        #endregion

        #region _getLight
        private async Task<LightDto?> _getLight(LightLocation lightLocation)
        {
            GetLightByLocationRequest getLightByLocationRequest = new GetLightByLocationRequest
            {
                Location = lightLocation,
            };

            GetLightByLocationResponse getLightByLocationResponse = await _lightRepository.GetLightByLocation(getLightByLocationRequest);

            return getLightByLocationResponse.IsSuccess ? getLightByLocationResponse.LightDto : null;
        }
        #endregion
    }
}
