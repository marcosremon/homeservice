using HomeService.Transversal.Wrapper.Base;
using RainSensorEntity = HomeService.Domain.Model.Entity.RainSensor;

namespace HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.GetRainingSensor;

public class GetRainingSensorResponse : BaseResponse
{
    public List<RainSensorEntity> RainingSensors { get; set; } = [];
}
