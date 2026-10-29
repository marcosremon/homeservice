using HomeService.Application.DataTransferObject.Light.GetLightByLocation;
using HomeService.Application.DataTransferObject.Light.PatchLightStatus;

namespace HomeService.Application.Interface.Repository;

public interface ILightRepository
{
    Task<GetLightByLocationResponse> GetLightByLocation(GetLightByLocationRequest getLightByLocationRequest);
    Task PatchLightStatus(PatchLightStatusRequest patchLightStatusRequest);
}
