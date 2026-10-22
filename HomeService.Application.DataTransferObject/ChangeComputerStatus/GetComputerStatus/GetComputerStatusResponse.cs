using HomeService.Transversal.Wrapper.Base;

namespace HomeService.Application.DataTransferObject.ChangeComputerStatus.GetComputerStatus;

public class GetComputerStatusResponse : BaseResponse
{
    public bool ComputerStatus { get; set; }
}
