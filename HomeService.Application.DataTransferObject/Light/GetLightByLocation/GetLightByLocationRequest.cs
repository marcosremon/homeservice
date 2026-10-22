using HomeService.Domain.Model.Enum.Light;

namespace HomeService.Application.DataTransferObject.Light.GetLightByLocation;

public class GetLightByLocationRequest
{
    public LightLocation Location { get; set; } = LightLocation.NONE;
}
