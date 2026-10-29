using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.CreateRainSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.GetRainingSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.RainSensor.PatchRainSensor;
using HomeService.Application.Interface.Repository;
using HomeService.Domain.Model.Entity;
using HomeService.Infraestructure.Persistence.Context;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.Wrapper.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
                // Una sola transaccion: zona, dispositivo y sensor se guardan juntos o nada.
                // Los SaveChanges intermedios son para obtener los id generados.
                await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();

                HouseZone? houseZone = await _context.HouseZones.FirstOrDefaultAsync(h => h.Callout == createRainSensorRequest.CallOut);

                if (houseZone is null)
                {
                    houseZone = new HouseZone { Callout = createRainSensorRequest.CallOut };

                    _context.HouseZones.Add(houseZone);
                    await _context.SaveChangesAsync();
                }

                long houseZoneId = houseZone.HouseZoneId;

                bool deviceExists = await _context.Devices.AnyAsync(d =>
                    d.HouseZoneId == houseZoneId &&
                    d.DeviceName == createRainSensorRequest.DeviceName &&
                    d.DeviceType == createRainSensorRequest.DeviceType);

                if (deviceExists)
                {
                    createRainSensorResponse.ResponseCode = ResponseCodes.CONFLICT;
                    createRainSensorResponse.IsSuccess = false;
                    createRainSensorResponse.Message = $"the device with macAddress {createRainSensorRequest.MacAddress} and device name {createRainSensorRequest.DeviceName} already exists";
                }
                else
                {
                    Device device = new Device
                    {
                        HouseZoneId = houseZoneId,
                        DeviceName = createRainSensorRequest.DeviceName,
                        DeviceType = createRainSensorRequest.DeviceType,
                        Model = createRainSensorRequest.Model,
                        Manufacturer = createRainSensorRequest.Manufacturer,
                        MacAddress = createRainSensorRequest.MacAddress,
                    };

                    _context.Devices.Add(device);
                    await _context.SaveChangesAsync();

                    DateTime measureAt = GeneralUtils.UtcNow();

                    RainSensor rainSensor = new RainSensor
                    {
                        DeviceId = device.DeviceId,
                        AdcValue = createRainSensorRequest.AdcValue,
                        WetnessPercent = createRainSensorRequest.WetnessPercent,
                        IsRaining = createRainSensorRequest.IsRaining,
                        MeasureAt = measureAt,
                        LastDetectedRain = createRainSensorRequest.IsRaining ? measureAt : DateTime.MinValue,
                    };

                    _context.RainSensors.Add(rainSensor);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    createRainSensorResponse.ResponseCode = ResponseCodes.CREATED;
                    createRainSensorResponse.IsSuccess = true;
                    createRainSensorResponse.Message = $"Rain Sensor with the name {createRainSensorRequest.DeviceName} created successfully.";
                }
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();

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
                HouseZone? houseZone = await _context.HouseZones.FirstOrDefaultAsync(h => h.Callout == patchRainSensorRequest.CallOut);

                if (houseZone is null)
                {
                    patchRainSensorResponse.ResponseCode = ResponseCodes.NOT_FOUND;
                    patchRainSensorResponse.IsSuccess = false;
                    patchRainSensorResponse.Message = $"House zone {patchRainSensorRequest.CallOut} not found";
                }
                else
                {
                    long houseZoneId = houseZone.HouseZoneId;

                    Device? device = await _context.Devices.FirstOrDefaultAsync(d =>
                        d.HouseZoneId == houseZoneId &&
                        d.DeviceName == patchRainSensorRequest.DeviceName &&
                        d.DeviceType == patchRainSensorRequest.DeviceType);

                    if (device is null)
                    {
                        patchRainSensorResponse.ResponseCode = ResponseCodes.NOT_FOUND;
                        patchRainSensorResponse.IsSuccess = false;
                        patchRainSensorResponse.Message = $"Device {patchRainSensorRequest.DeviceName} not found";
                    }
                    else
                    {
                        long deviceId = device.DeviceId;
                        RainSensor? rainSensor = await _context.RainSensors.FirstOrDefaultAsync(r => r.DeviceId == deviceId);

                        if (rainSensor is null)
                        {
                            patchRainSensorResponse.ResponseCode = ResponseCodes.NOT_FOUND;
                            patchRainSensorResponse.IsSuccess = false;
                            patchRainSensorResponse.Message = $"No rain sensor found with the name {device.DeviceName}";
                        }
                        else
                        {
                            if (!GeneralUtils.IsNullOrEmpty(patchRainSensorRequest.Model))
                                device.Model = patchRainSensorRequest.Model;

                            if (!GeneralUtils.IsNullOrEmpty(patchRainSensorRequest.MacAddress))
                                device.MacAddress = patchRainSensorRequest.MacAddress;

                            DateTime measureAt = GeneralUtils.UtcNow();
                            bool wasRaining = rainSensor.IsRaining;

                            rainSensor.AdcValue = patchRainSensorRequest.AdcValue;
                            rainSensor.WetnessPercent = patchRainSensorRequest.WetnessPercent;
                            rainSensor.IsRaining = patchRainSensorRequest.IsRaining;
                            rainSensor.MeasureAt = measureAt;

                            if (patchRainSensorRequest.IsRaining)
                                rainSensor.LastDetectedRain = measureAt;

                            await _context.SaveChangesAsync();

                            // Solo cuenta como "empieza a llover" el paso de seco a mojado: es lo
                            // que dispara la notificacion en RainSensorApplication.
                            patchRainSensorResponse.RainStarted = !wasRaining && patchRainSensorRequest.IsRaining;
                            patchRainSensorResponse.ResponseCode = ResponseCodes.OK;
                            patchRainSensorResponse.IsSuccess = true;
                            patchRainSensorResponse.Message = $"Rain Sensor with the name {device.DeviceName} updated successfully.";
                        }
                    }
                }
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
                getRainingSensorResponse.RainingSensors = await _context.RainSensors.Where(r => r.IsRaining).ToListAsync();

                getRainingSensorResponse.ResponseCode = ResponseCodes.OK;
                getRainingSensorResponse.IsSuccess = true;
                getRainingSensorResponse.Message = $"raining sensors count: {getRainingSensorResponse.RainingSensors.Count}";
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

        #region ExpireRainStatus
        // Un UPDATE directo en la base de datos, sin cargar las entidades.
        public async Task ExpireRainStatus(DateTime expiryLimit)
        {
            try
            {
                int expired = await _context.RainSensors
                    .Where(r => r.IsRaining && r.LastDetectedRain < expiryLimit)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.IsRaining, false));

                if (expired > 0)
                    Console.WriteLine($"RainSensorRepository -> ExpireRainStatus: {expired} sensor(es) dejan de marcar lluvia");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unexpected error on RainSensorRepository -> ExpireRainStatus: {ex.Message}");
            }
        }
        #endregion
    }
}
