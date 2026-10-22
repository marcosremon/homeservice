using HomeService.Application.DataTransferObject.Roomba.CreateRoomba;
using HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;

namespace HomeService.Application.Interface.Application;

public interface IRoombaApplication
{
    Task<CreateRoombaResponse> CreateRoomba(CreateRoombaRequest createRoombaRequest);

    Task<PatchRoombaStateResponse> PatchRoombaState(PatchRoombaStateRequest patchRoombaStateRequest);
}
