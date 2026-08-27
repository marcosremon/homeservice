import asyncio

from aiomqtt import Client, MqttError

from application.interface.service.IMqttService import IMqttService
from transversal.common.configuration.Settings import Settings

_CLIENT_ID: str = "HomeLabServer"
_QOS_AT_LEAST_ONCE: int = 1

class MqttService(IMqttService):

    def __init__(self, settings: Settings):
        self._settings: Settings = settings
        self._client: Client | None = None
        self._connectionLock: asyncio.Lock = asyncio.Lock()

    # region Publish
    async def Publish(self, topic: str, payload: str) -> None:
        try:
            client: Client = await self._connect()
            await client.publish(topic, payload = payload, qos = _QOS_AT_LEAST_ONCE, retain = True)
        except MqttError as ex:
            print(f"MqttService -> Publish -> reconectando tras: {ex}")

            await self._disconnect()

            client = await self._connect()
            await client.publish(topic, payload = payload, qos = _QOS_AT_LEAST_ONCE, retain = True)
    # endregion

    # region _connect
    async def _connect(self) -> Client:
        async with self._connectionLock:
            if self._client is not None:
                return self._client

            client: Client = Client(
                hostname = self._settings.mqttHost,
                port = self._settings.mqttPort,
                username = self._settings.mqttUser or None,
                password = self._settings.mqttPassword or None,
                identifier = _CLIENT_ID,
                clean_session = True,
            )

            await client.__aenter__()
            self._client = client

            return client
    # endregion

    # region _disconnect
    async def _disconnect(self) -> None:
        """Cierra la conexion. Se llama al parar la app y antes de reconectar."""
        async with self._connectionLock:
            if self._client is None:
                return

            try:
                await self._client.__aexit__(None, None, None)
            except Exception as ex:
                print(f"MqttService -> _disconnect -> {ex}")
            finally:
                self._client = None
    # endregion