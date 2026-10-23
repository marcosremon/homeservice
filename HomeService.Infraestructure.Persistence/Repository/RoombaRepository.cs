using HomeService.Application.DataTransferObject.Roomba.CreateRoomba;
using HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;
using HomeService.Application.Interface.Repository;
using HomeService.Infraestructure.Persistence.Context;
using HomeService.Transversal.Wrapper.Base;

namespace HomeService.Infraestructure.Persistence.Repository
{
    public class RoombaRepository : IRoombaRepository
    {
        private readonly ApplicationDbContext _context;

        public RoombaRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        #region CreateRoomba
        public async Task<CreateRoombaResponse> CreateRoomba(CreateRoombaRequest createRoombaRequest)
        {
            CreateRoombaResponse createRoombaResponse = new CreateRoombaResponse();

            try
            {
            }
            catch (Exception ex)
            {
                // Equivale al session.rollback(): descarta lo pendiente del contexto.
                _context.ChangeTracker.Clear();

                createRoombaResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                createRoombaResponse.IsSuccess = false;
                createRoombaResponse.Message = $"Unexpected error on RoombaRepository -> CreateRoomba: {ex.Message}";
            }

            return createRoombaResponse;
        }
        #endregion

        #region PatchRoombaState
        public async Task<PatchRoombaStateResponse> PatchRoombaState(PatchRoombaStateRequest patchRoombaStateRequest)
        {
            PatchRoombaStateResponse patchRoombaStateResponse = new PatchRoombaStateResponse();

            try
            {
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();

                patchRoombaStateResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                patchRoombaStateResponse.IsSuccess = false;
                patchRoombaStateResponse.Message = $"Unexpected error on RoombaRepository -> PatchRoombaState: {ex.Message}";
            }

            return patchRoombaStateResponse;
        }
        #endregion
    }
}
