using HomeService.Application.Interface.Service;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Common.Utils;

namespace HomeService.Infraestructure.Gateway
{
    public class ComputerStatusService : IComputerStatusService
    {
        private readonly Settings _settings;

        public ComputerStatusService(Settings settings)
        {
            _settings = settings;
        }

        #region ExecuteComputerStatusOrder
        // Solo informa del estado: ni enciende ni apaga. Encender y apagar lo hace
        // ChangeComputerStatusRepository desde los endpoints REST.
        public async Task<string> ExecuteComputerStatusOrder(string intentName)
        {
            bool computerStatus = await ComputerStatusUtils.ComputerStatus(_settings.LanComputerIp);

            switch (intentName)
            {
                case "computer_status_order_encender_ordenador" when computerStatus:
                    return "El ordenador ya está encendido.";

                case "computer_status_order_apagar_ordenador" when !computerStatus:
                    return "El ordenador ya está apagado.";

                case "computer_status_order_get_status":
                    return computerStatus ? "El ordenador está encendido." : "El ordenador está apagado.";

                default:
                    return string.Empty;
            }
        }
        #endregion
    }
}
