using HomeService.Transversal.Wrapper.Base;

namespace HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.GetPresenceSensorsStatus;

public class GetPresenceSensorsStatusResponse : BaseResponse
{
    public bool IsHouseEmpty { get; set; }
    public DateTime LastRoombaActivation { get; set; } = DateTime.MinValue;
}
