using HomeService.Application.DataTransferObject.Roomba.CreateRoomba;
using HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;
using HomeService.Application.Interface.Application;
using HomeService.Application.Interface.Repository;

namespace HomeService.Application.UseCase;

public class RoombaApplication : IRoombaApplication
{
    private readonly IRoombaRepository _roombaRepository;

    public RoombaApplication(IRoombaRepository roombaRepository)
    {
        _roombaRepository = roombaRepository;
    }

    #region CreateRoomba
    public Task<CreateRoombaResponse> CreateRoomba(CreateRoombaRequest createRoombaRequest) =>
        _roombaRepository.CreateRoomba(createRoombaRequest);
    #endregion

    #region PatchRoombaState
    public Task<PatchRoombaStateResponse> PatchRoombaState(PatchRoombaStateRequest patchRoombaStateRequest) =>
        _roombaRepository.PatchRoombaState(patchRoombaStateRequest);
    #endregion
}
