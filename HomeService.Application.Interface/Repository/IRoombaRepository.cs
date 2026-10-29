using HomeService.Application.DataTransferObject.Roomba.CreateRoomba;
using HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;

namespace HomeService.Application.Interface.Repository;

public interface IRoombaRepository
{
    Task<CreateRoombaResponse> CreateRoomba(CreateRoombaRequest createRoombaRequest);
    Task<PatchRoombaStateResponse> PatchRoombaState(PatchRoombaStateRequest patchRoombaStateRequest);
}
