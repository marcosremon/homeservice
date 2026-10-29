using HomeService.Application.Interface.Service;
using HomeService.Transversal.Common.Configuration;
using HomeService.Transversal.Common.Utils;
using MQTTnet;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;

namespace HomeService.Infraestructure.Gateway
{
    // Singleton: la conexion con el broker se comparte entre peticiones. El
    // contenedor llama a DisposeAsync al parar la app y ahi se cierra.
    public class MqttService : IMqttService, IAsyncDisposable
    {
        private const string _clientId = "HomeLabServer";

        private readonly Settings _settings;
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);
        private IMqttClient? _client;

        public MqttService(Settings settings)
        {
            _settings = settings;
        }

        #region Publish
        // retain = false para comandos relativos: el broker reentrega el ultimo
        // mensaje retenido a quien se suscriba, asi que un "+2" retenido se volveria
        // a aplicar cada vez que el dispositivo se reconecta. Solo tiene sentido
        // retener estados absolutos (ON / OFF).
        public async Task Publish(string topic, string payload, bool retain = true)
        {
            MqttApplicationMessage message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(payload)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .WithRetainFlag(retain)
                .Build();

            try
            {
                IMqttClient client = await _connect();
                await client.PublishAsync(message);
            }
            catch (MqttCommunicationException ex)
            {
                Console.WriteLine($"MqttService -> Publish -> reconectando tras: {ex.Message}");

                await _disconnect();

                IMqttClient client = await _connect();
                await client.PublishAsync(message);
            }
        }
        #endregion

        #region _connect
        private async Task<IMqttClient> _connect()
        {
            await _connectionLock.WaitAsync();

            try
            {
                if (_client is not null && _client.IsConnected)
                    return _client;

                if (_client is not null)
                {
                    _client.Dispose();
                    _client = null;
                }

                MqttClientOptionsBuilder optionsBuilder = new MqttClientOptionsBuilder()
                    .WithTcpServer(_settings.MqttHost, _settings.MqttPort)
                    .WithClientId(_clientId)
                    .WithCleanSession(true);

                // Sin usuario se conecta como anonimo.
                if (!GeneralUtils.IsNullOrEmpty(_settings.MqttUser))
                    optionsBuilder.WithCredentials(_settings.MqttUser, _settings.MqttPassword);

                IMqttClient client = new MqttClientFactory().CreateMqttClient();

                try
                {
                    await client.ConnectAsync(optionsBuilder.Build(), CancellationToken.None);
                }
                catch
                {
                    client.Dispose();
                    throw;
                }

                _client = client;

                return client;
            }
            finally
            {
                _connectionLock.Release();
            }
        }
        #endregion

        #region _disconnect
        private async Task _disconnect()
        {
            await _connectionLock.WaitAsync();

            try
            {
                if (_client is null)
                    return;

                try
                {
                    if (_client.IsConnected)
                        await _client.DisconnectAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"MqttService -> _disconnect -> {ex.Message}");
                }
                finally
                {
                    _client.Dispose();
                    _client = null;
                }
            }
            finally
            {
                _connectionLock.Release();
            }
        }
        #endregion

        #region DisposeAsync
        public async ValueTask DisposeAsync()
        {
            await _disconnect();
            _connectionLock.Dispose();
        }
        #endregion
    }
}
