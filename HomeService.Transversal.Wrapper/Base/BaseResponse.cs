namespace HomeService.Transversal.Wrapper.Base;

public class BaseResponse
{
    public ResponseCodes ResponseCode { get; set; } = ResponseCodes.UNEXPECTED_ERROR;
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}
