using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.CreateTemperatureSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.PatchTemperatureSensor;
using HomeService.Application.Interface.Repository;
using HomeService.Infraestructure.Persistence.Context;
using HomeService.Transversal.Wrapper.Base;

namespace HomeService.Infraestructure.Persistence.Repository
{
    public class TemperatureSensorRepository : ITemperatureSensorRepository
    {
        private readonly ApplicationDbContext _context;

        public TemperatureSensorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        #region CreateTemperatureSensor
        public async Task<CreateTemperatureSensorResponse> CreateTemperatureSensor(CreateTemperatureSensorRequest createTemperatureSensorRequest)
        {
            CreateTemperatureSensorResponse createTemperatureSensorResponse = new CreateTemperatureSensorResponse();

            try
            {
            }
            catch (Exception ex)
            {
                createTemperatureSensorResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                createTemperatureSensorResponse.IsSuccess = false;
                createTemperatureSensorResponse.Message = $"Unexpected error on TemperatureSensorRepository -> CreateTemperatureSensor: {ex.Message}";
            }

            return createTemperatureSensorResponse;
        }
        #endregion

        #region PatchTemperatureSensor
        public async Task<PatchTemperatureSensorResponse> PatchTemperatureSensor(PatchTemperatureSensorRequest patchTemperatureSensorRequest)
        {
            PatchTemperatureSensorResponse patchTemperatureSensorResponse = new PatchTemperatureSensorResponse();

            try
            {
            }
            catch (Exception ex)
            {
                // Equivale al session.rollback(): descarta lo pendiente del contexto.
                _context.ChangeTracker.Clear();

                patchTemperatureSensorResponse.ResponseCode = ResponseCodes.UNEXPECTED_ERROR;
                patchTemperatureSensorResponse.IsSuccess = false;
                patchTemperatureSensorResponse.Message = $"Unexpected error on TemperatureSensorRepository -> PatchTemperatureSensor: {ex.Message}";
            }

            return patchTemperatureSensorResponse;
        }
        #endregion
    }
}
