using System.Text.Json.Serialization;

namespace HomeService.Transversal.Common.Roomba.Payload;

// Cuerpo del mensaje MQTT que se publica en cmd/{blid}/delta.
//
// Los alias no son decorativos: el protocolo de iRobot espera pmap_id y
// user_pmapv_id en snake_case, asi que el nombre de la propiedad y el del JSON
// van por separado.
//
// Los campos opcionales son null a proposito: al serializar desaparecen del JSON
// por el WhenWritingNull. La roomba rechaza el comando si le llegan campos de
// region vacios.
public class RoombaPayload
{
    [JsonPropertyName("command")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Command { get; set; }

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("initiator")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Initiator { get; set; }

    [JsonPropertyName("ordered")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Ordered { get; set; }

    [JsonPropertyName("pmap_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PmapId { get; set; }

    [JsonPropertyName("user_pmapv_id")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserPmapvId { get; set; }

    [JsonPropertyName("regions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<RoombaRegion>? Regions { get; set; }
}
