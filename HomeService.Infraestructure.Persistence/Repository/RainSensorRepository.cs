using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.CreateRainSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.GetRainingSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.PatchRainSensor;
using HomeService.Application.Interface.Repository;
using HomeService.Infraestructure.Persistence.Context;
using HomeService.Transversal.Wrapper.Base;

namespace HomeService.Infraestructure.Persistence.Repository
{
    public class RainSensorRepository : IRainSensorRepository
    {
        private readonly ApplicationDbContext _context;

        public RainSensorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        #region CreateRainSensor
        public async Task<CreateRainSensorResponse> CreateRainSensor(CreateRainSensorRequest createRainSensorRequest)
        {
            CreateRainSensorResponse createRainSensorResponse = new CreateRainSensorResponse();

            try
            {
            }
            catch (Exception ex)
            {
                createRainSensorResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                createRainSensorResponse.IsSuccess = false;
                createRainSensorResponse.Message = $"Unexpected error on RainSensorRepository -> CreateRainSensor: {ex.Message}";
            }

            return createRainSensorResponse;
        }
        #endregion

        #region PatchRainSensor
        public async Task<PatchRainSensorResponse> PatchRainSensor(PatchRainSensorRequest patchRainSensorRequest)
        {
            PatchRainSensorResponse patchRainSensorResponse = new PatchRainSensorResponse();

            try
            {
            }
            catch (Exception ex)
            {
                // Equivale al session.rollback(): descarta lo pendiente del contexto.
                _context.ChangeTracker.Clear();

                patchRainSensorResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                patchRainSensorResponse.IsSuccess = false;
                patchRainSensorResponse.Message = $"Unexpected error on RainSensorRepository -> PatchRainSensor: {ex.Message}";
            }

            return patchRainSensorResponse;
        }
        #endregion

        #region GetRainingSensor
        public async Task<GetRainingSensorResponse> GetRainingSensor()
        {
            GetRainingSensorResponse getRainingSensorResponse = new GetRainingSensorResponse();

            try
            {
            }
            catch (Exception ex)
            {
                getRainingSensorResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                getRainingSensorResponse.IsSuccess = false;
                getRainingSensorResponse.Message = $"Unexpected error on RainSensorRepository -> GetRainingSensor: {ex.Message}";
            }

            return getRainingSensorResponse;
        }
        #endregion
    }
}
