using System.Runtime.CompilerServices;
using HomeService.Infraestructure.Persistence.CreateDatabase;

namespace HomeService.Service.UnitTests.TestSetup
{
    // Settings.GetSettings() lee el .env real subiendo desde el binario y lo cachea
    // para todo el proceso. Las variables de entorno tienen prioridad, asi que antes
    // de que nadie lo toque se apunta todo lo externo a puertos cerrados de
    // 127.0.0.1, igual que en docs/runbooks/probar-en-local.md. Valores falsos pero
    // no vacios: en Windows una variable vacia se borra y ganaria el .env.
    public static class TestEnvironment
    {
        public const string InternalApiKey = "test-internal-api-key";
        public const string DebugBypassKey = "test-debug-bypass-key";
        public const string AlexaSkillId = "amzn1.ask.skill.test-skill";
        public const string AlexaVersion = "1.0";

        #region Initialize
        [ModuleInitializer]
        public static void Initialize()
        {
            Dictionary<string, string> variables = new Dictionary<string, string>
            {
                ["DATABASE_URL"] = "Host=127.0.0.1;Port=1;Database=unit_tests;Username=unit_tests;Password=unit_tests;Timeout=1",
                ["INTERNAL_API_KEY"] = InternalApiKey,
                ["DEBUG_BYPASS_KEY"] = DebugBypassKey,
                ["ALEXA_SKILL_ID"] = AlexaSkillId,
                ["ALEXA_VERSION"] = AlexaVersion,
                ["MQTT_HOST"] = "127.0.0.1",
                ["MQTT_PORT"] = "1",
                ["ROOMBA_ID"] = "127.0.0.1",
                ["ROOMBA_PORT"] = "1",
                ["ROOMBA_BLID"] = "unit_tests",
                ["ROOMBA_PASSWD"] = "unit_tests",
                ["NTFY_BASE_URL"] = "http://127.0.0.1:1",
                ["NTFY_TOPIC"] = "unit_tests",
                ["GEMINI_API_KEY"] = "unit_tests",
                ["GEMINI_MODEL"] = "unit_tests",
                ["LAN_COMPUTER_IP"] = "127.0.0.1",
                ["LAN_COMPUTER_MAC"] = "00:00:5E:00:53:01",
                ["LAN_BROADCAST_IP"] = "127.0.0.1",
                ["LAN_CACHYOS_USER"] = "unit_tests",
            };

            foreach (KeyValuePair<string, string> variable in variables)
                Environment.SetEnvironmentVariable(variable.Key, variable.Value);

            // Program.cs llama al migrador al arrancar: sin reintentos se rinde al
            // primer "connection refused" en vez de esperar 20 segundos.
            DatabaseMigrator.maximumNumberOfConnectionRetries = 0;
            DatabaseMigrator.timeWaitRetryMs = 0;
        }
        #endregion
    }
}
