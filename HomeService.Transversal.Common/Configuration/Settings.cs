using HomeService.Transversal.Common.Utils;
using Microsoft.Extensions.Configuration;

namespace HomeService.Transversal.Common.Configuration;

public class Settings
{
    private static readonly Lazy<Settings> Cached = new(Build);

    #region properties

    [ConfigurationKeyName("APP_HOST")]
    public string AppHost { get; set; } = "0.0.0.0";

    [ConfigurationKeyName("APP_PORT")]
    public int AppPort { get; set; } = 8000;

    // postgresql://usuario:password@host:puerto/base_de_datos
    [ConfigurationKeyName("DATABASE_URL")]
    public string DatabaseUrl { get; set; } = string.Empty;

    [ConfigurationKeyName("INTERNAL_API_KEY")]
    public string InternalApiKey { get; set; } = string.Empty;

    [ConfigurationKeyName("DEBUG_BYPASS_KEY")]
    public string DebugBypassKey { get; set; } = string.Empty;

    [ConfigurationKeyName("ALEXA_VERSION")]
    public string AlexaVersion { get; set; } = "1.0";

    [ConfigurationKeyName("ALEXA_SKILL_ID")]
    public string AlexaSkillId { get; set; } = string.Empty;

    [ConfigurationKeyName("GEMINI_API_KEY")]
    public string GeminiApiKey { get; set; } = string.Empty;

    [ConfigurationKeyName("GEMINI_MODEL")]
    public string GeminiModel { get; set; } = string.Empty;

    [ConfigurationKeyName("MQTT_HOST")]
    public string MqttHost { get; set; } = string.Empty;

    [ConfigurationKeyName("MQTT_PORT")]
    public int MqttPort { get; set; } = 1883;

    [ConfigurationKeyName("MQTT_USER")]
    public string MqttUser { get; set; } = string.Empty;

    [ConfigurationKeyName("MQTT_PASSWORD")]
    public string MqttPassword { get; set; } = string.Empty;

    [ConfigurationKeyName("ROOMBA_ID")]
    public string RoombaId { get; set; } = string.Empty;

    [ConfigurationKeyName("ROOMBA_PORT")]
    public string RoombaPort { get; set; } = "8883";

    [ConfigurationKeyName("ROOMBA_BLID")]
    public string RoombaBlid { get; set; } = string.Empty;

    [ConfigurationKeyName("ROOMBA_PASSWD")]
    public string RoombaPasswd { get; set; } = string.Empty;

    [ConfigurationKeyName("ROOMBA_PMAP_ID")]
    public string RoombaPmapId { get; set; } = string.Empty;

    [ConfigurationKeyName("ROOMBA_PMAP_VERSION")]
    public string RoombaPmapVersion { get; set; } = string.Empty;

    // cualquiera que lo conozca puede publicar y leer, por eso el topic no tiene default.
    [ConfigurationKeyName("NTFY_BASE_URL")]
    public string NtfyBaseUrl { get; set; } = "https://ntfy.sh";

    [ConfigurationKeyName("NTFY_TOPIC")]
    public string NtfyTopic { get; set; } = string.Empty;

    [ConfigurationKeyName("LAN_COMPUTER_IP")]
    public string LanComputerIp { get; set; } = string.Empty;

    [ConfigurationKeyName("LAN_COMPUTER_MAC")]
    public string LanComputerMac { get; set; } = string.Empty;

    [ConfigurationKeyName("LAN_BROADCAST_IP")]
    public string LanBroadcastIp { get; set; } = string.Empty;

    [ConfigurationKeyName("LAN_CACHYOS_USER")]
    public string LanCachyosUser { get; set; } = string.Empty;

    #endregion

    #region GetSettings
    // Equivalente al @lru_cache del GetSettings() de python: una sola lectura por proceso.
    public static Settings GetSettings() => Cached.Value;
    #endregion

    #region Build
    private static Settings Build()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(ReadDotEnv())
            .AddEnvironmentVariables()
            .Build();

        Settings settings = configuration.Get<Settings>() ?? new Settings();

        // En pydantic estas dos no tienen default, asi que faltar es un error de arranque.
        if (GeneralUtils.IsNullOrWhiteSpace(settings.DatabaseUrl))
            throw new InvalidOperationException("Falta la variable de entorno DATABASE_URL.");

        if (GeneralUtils.IsNullOrWhiteSpace(settings.InternalApiKey))
            throw new InvalidOperationException("Falta la variable de entorno INTERNAL_API_KEY.");

        return settings;
    }
    #endregion

    #region ReadDotEnv
    // .NET no lee .env por si solo: se busca subiendo desde el binario, como hace el
    // env_file de pydantic. Las variables de entorno reales tienen prioridad.
    private static Dictionary<string, string?> ReadDotEnv()
    {
        Dictionary<string, string?> values = [];

        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, ".env")))
            directory = directory.Parent;

        if (directory is null)
            return values;

        foreach (string line in File.ReadLines(Path.Combine(directory.FullName, ".env")))
        {
            string trimmed = line.Trim();

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            int separator = trimmed.IndexOf('=');
            if (separator <= 0)
                continue;

            values[trimmed[..separator].Trim()] = trimmed[(separator + 1)..].Trim().Trim('"');
        }

        return values;
    }
    #endregion
}
