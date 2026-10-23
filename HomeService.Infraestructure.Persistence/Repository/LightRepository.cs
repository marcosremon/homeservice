using HomeService.Application.DataTransferObject.Light.GetLightByLocation;
using HomeService.Application.DataTransferObject.Light.PatchLightStatus;
using HomeService.Application.Interface.Repository;
using HomeService.Infraestructure.Persistence.Context;
using HomeService.Transversal.Wrapper.Base;

namespace HomeService.Infraestructure.Persistence.Repository
{
    public class LightRepository : ILightRepository
    {
        private readonly ApplicationDbContext _context;

        public LightRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        #region GetLightByLocation
        public async Task<GetLightByLocationResponse> GetLightByLocation(GetLightByLocationRequest getLightByLocationRequest)
        {
            GetLightByLocationResponse getLightByLocationResponse = new GetLightByLocationResponse();

            try
            {
            }
            catch (Exception ex)
            {
                getLightByLocationResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                getLightByLocationResponse.IsSuccess = false;
                getLightByLocationResponse.Message = $"Unexpected error on LightRepository -> GetLightByLocation: {ex.Message}";
            }

            return getLightByLocationResponse;
        }
        #endregion

        #region PatchLightStatus
        public async Task PatchLightStatus(PatchLightStatusRequest patchLightStatusRequest)
        {
            try
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error on LightRepository -> PatchLightStatus: {ex.Message}");
            }
        }
        #endregion
    }
}
