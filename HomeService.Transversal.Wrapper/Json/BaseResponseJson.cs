using System.Text.Json.Serialization;

namespace HomeService.Transversal.Wrapper.Json;

public class BaseResponseJson
{
    [JsonPropertyName("responseCodeJson")]
    public ResponseCodesJson ResponseCodeJson { get; set; } = ResponseCodesJson.UNEXPECTED_ERROR;

    [JsonPropertyName("isSuccess")]
    public bool IsSuccess { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}
