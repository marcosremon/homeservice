using HomeService.Transversal.Wrapper.Base;

namespace HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.PatchRainSensor;

public class PatchRainSensorResponse : BaseResponse
{
    public bool RainStarted { get; set; }
}
