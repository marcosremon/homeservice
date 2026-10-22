using HomeService.Domain.Model.Enum.Light;

namespace HomeService.Application.DataTransferObject.Light.PatchLightStatus;

public class PatchLightStatusRequest
{
    public required LightLocation LightLocation { get; set; }
    public required bool IsOn { get; set; }
}
