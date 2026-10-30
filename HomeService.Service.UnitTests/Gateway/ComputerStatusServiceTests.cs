using HomeService.Infraestructure.Gateway;
using HomeService.Transversal.Common.Configuration;

namespace HomeService.Service.UnitTests.Gateway
{
    // "Encendido" es un ping a 127.0.0.1; "apagado", una IP invalida que ni se pinguea.
    public class ComputerStatusServiceTests
    {
        private const string _onIp = "127.0.0.1";
        private const string _offIp = "no-es-una-ip";

        #region ExecuteComputerStatusOrder
        [Theory]
        [InlineData(_onIp, "computer_status_order_get_status", "El ordenador está encendido.")]
        [InlineData(_offIp, "computer_status_order_get_status", "El ordenador está apagado.")]
        [InlineData(_onIp, "computer_status_order_encender_ordenador", "El ordenador ya está encendido.")]
        [InlineData(_offIp, "computer_status_order_apagar_ordenador", "El ordenador ya está apagado.")]
        [InlineData(_offIp, "computer_status_order_encender_ordenador", "")]
        [InlineData(_onIp, "computer_status_order_apagar_ordenador", "")]
        [InlineData(_onIp, "computer_status_order_reiniciar", "")]
        public async Task ExecuteComputerStatusOrder(string computerIp, string intentName, string expected)
        {
            ComputerStatusService computerStatusService = new ComputerStatusService(new Settings { LanComputerIp = computerIp });

            Assert.Equal(expected, await computerStatusService.ExecuteComputerStatusOrder(intentName));
        }
        #endregion
    }
}
