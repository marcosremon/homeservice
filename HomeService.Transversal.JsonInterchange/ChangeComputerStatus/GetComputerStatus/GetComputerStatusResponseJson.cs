using System.Text.Json.Serialization;
using HomeService.Transversal.Wrapper.Json;

namespace HomeService.Transversal.JsonInterchange.ChangeComputerStatus.GetComputerStatus;

public class GetComputerStatusResponseJson : BaseResponseJson
{
    [JsonPropertyName("ComputerStatus")]
    public bool ComputerStatus { get; set; }
}
