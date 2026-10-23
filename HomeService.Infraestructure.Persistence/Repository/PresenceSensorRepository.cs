using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.CreatePresenceSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.GetPresenceSensorsStatus;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.PatchPresenceSensorData;
using HomeService.Application.Interface.Repository;
using HomeService.Infraestructure.Persistence.Context;
using HomeService.Transversal.Wrapper.Base;

namespace HomeService.Infraestructure.Persistence.Repository
{
    public class PresenceSensorRepository : IPresenceSensorRepository
    {
        private readonly ApplicationDbContext _context;

        public PresenceSensorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        #region CreatePresenceSensor
        public async Task<CreatePresenceSensorResponse> CreatePresenceSensor(CreatePresenceSensorRequest createPresenceSensorRequest)
        {
            CreatePresenceSensorResponse createPresenceSensorResponse = new CreatePresenceSensorResponse();

            try
            {
            }
            catch (Exception ex)
            {
                createPresenceSensorResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                createPresenceSensorResponse.IsSuccess = false;
                createPresenceSensorResponse.Message = $"Unexpected error on PresenceSensorRepository -> CreatePresenceSensor: {ex.Message}";
            }

            return createPresenceSensorResponse;
        }
        #endregion

        #region PatchPresenceSensorData
        public async Task<PatchPresenceSensorDataResponse> PatchPresenceSensorData(PatchPresenceSensorDataRequest patchPresenceSensorDataRequest)
        {
            PatchPresenceSensorDataResponse patchPresenceSensorDataResponse = new PatchPresenceSensorDataResponse();

            try
            {
            }
            catch (Exception ex)
            {
                // Equivale al session.rollback(): descarta lo pendiente del contexto.
                _context.ChangeTracker.Clear();

                patchPresenceSensorDataResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                patchPresenceSensorDataResponse.IsSuccess = false;
                patchPresenceSensorDataResponse.Message = $"Unexpected error on PresenceSensorRepository -> PatchPresenceSensorData: {ex.Message}";
            }

            return patchPresenceSensorDataResponse;
        }
        #endregion

        #region GetPresenceSensorsStatus
        public async Task<GetPresenceSensorsStatusResponse> GetPresenceSensorsStatus()
        {
            GetPresenceSensorsStatusResponse getPresenceSensorsStatusResponse = new GetPresenceSensorsStatusResponse();

            try
            {
            }
            catch (Exception ex)
            {
                getPresenceSensorsStatusResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                getPresenceSensorsStatusResponse.IsSuccess = false;
                getPresenceSensorsStatusResponse.Message = $"Unexpected error on PresenceSensorRepository -> GetPresenceSensorsStatus: {ex.Message}";
            }

            return getPresenceSensorsStatusResponse;
        }
        #endregion
    }
}
