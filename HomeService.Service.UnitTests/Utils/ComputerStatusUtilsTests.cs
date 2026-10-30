using System.Net;
using System.Net.Sockets;
using HomeService.Transversal.Common.Utils;

namespace HomeService.Service.UnitTests.Utils
{
    // Todo contra 127.0.0.1 o contra valores invalidos: nunca sale de este equipo. En
    // la coleccion WakeOnLan para no compartir el puerto UDP 9 con otros tests.
    [Collection("WakeOnLan")]
    public class ComputerStatusUtilsTests
    {
        #region ComputerStatus_InvalidIp_ReturnsFalseWithoutPinging
        [Theory]
        [InlineData("")]
        [InlineData("no-es-una-ip")]
        [InlineData("127.0.0.1; rm -rf /")]
        [InlineData("999.1.1.1")]
        public async Task ComputerStatus_InvalidIp_ReturnsFalseWithoutPinging(string ipAddress)
        {
            Assert.False(await ComputerStatusUtils.ComputerStatus(ipAddress));
        }
        #endregion

        #region ComputerStatus_Localhost_ReturnsTrue
        [Fact]
        public async Task ComputerStatus_Localhost_ReturnsTrue()
        {
            Assert.True(await ComputerStatusUtils.ComputerStatus("127.0.0.1"));
        }
        #endregion

        #region SendWakeOnLanPacket_InvalidMac_Throws
        [Theory]
        [InlineData("")]
        [InlineData("00:00:5E:00:53")]
        [InlineData("00:00:5E:00:53:ZZ")]
        [InlineData("00005E005301")]
        [InlineData("00:00:5E:00:53:01:02")]
        public async Task SendWakeOnLanPacket_InvalidMac_Throws(string macAddress)
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => ComputerStatusUtils.SendWakeOnLanPacket(macAddress, "127.0.0.1"));

            Assert.Equal("macAddress", exception.ParamName);
        }
        #endregion

        #region SendWakeOnLanPacket_InvalidBroadcast_Throws
        [Theory]
        [InlineData("")]
        [InlineData("broadcast")]
        public async Task SendWakeOnLanPacket_InvalidBroadcast_Throws(string broadcastIp)
        {
            ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() => ComputerStatusUtils.SendWakeOnLanPacket("00:00:5E:00:53:01", broadcastIp));

            Assert.Equal("broadcastIp", exception.ParamName);
        }
        #endregion

        #region SendWakeOnLanPacket_SendsMagicPacketToPort9
        // El paquete magico: 6 bytes 0xFF y la MAC repetida 16 veces, a UDP 9.
        [Theory]
        [InlineData("00:00:5E:00:53:01")]
        [InlineData("00-00-5e-00-53-01")]
        public async Task SendWakeOnLanPacket_SendsMagicPacketToPort9(string macAddress)
        {
            using UdpClient listener = new UdpClient(new IPEndPoint(IPAddress.Loopback, 9));

            Task<UdpReceiveResult> receive = listener.ReceiveAsync();
            await ComputerStatusUtils.SendWakeOnLanPacket(macAddress, "127.0.0.1");

            Task completed = await Task.WhenAny(receive, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.Same(receive, completed);

            byte[] packet = (await receive).Buffer;
            byte[] macBytes = [0x00, 0x00, 0x5E, 0x00, 0x53, 0x01];

            Assert.Equal(6 + (16 * 6), packet.Length);
            Assert.All(packet[..6], value => Assert.Equal(0xFF, value));

            for (int repetition = 0; repetition < 16; repetition++)
                Assert.Equal(macBytes, packet[(6 + (repetition * 6))..(12 + (repetition * 6))]);
        }
        #endregion

        #region ShutdownComputer_ComputerAlreadyOff_DoesNotRunSsh
        [Fact]
        public async Task ShutdownComputer_ComputerAlreadyOff_DoesNotRunSsh()
        {
            string message = await ComputerStatusUtils.ShutdownComputer("no-es-una-ip", "usuario");

            Assert.Equal("El ordenador ya esta apagado.", message);
        }
        #endregion
    }
}
