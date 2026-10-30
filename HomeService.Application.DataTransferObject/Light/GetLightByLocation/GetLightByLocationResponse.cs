using HomeService.Transversal.Wrapper.Base;

namespace HomeService.Application.DataTransferObject.Light.GetLightByLocation;

public class GetLightByLocationResponse : BaseResponse
{
    public LightDto LightDto { get; set; } = new LightDto();
}
