using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.CreateTemperatureSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.TemperatureSensor.PatchTemperatureSensor;
using HomeService.Application.Interface.Repository;
using HomeService.Domain.Model.Entity;
using HomeService.Infraestructure.Persistence.Context;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.Wrapper.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
                // Una sola transaccion: zona, dispositivo y sensor se guardan juntos o nada.
                // Los SaveChanges intermedios son para obtener los id generados.
                await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();

                HouseZone? houseZone = await _context.HouseZones.FirstOrDefaultAsync(h => h.Callout == createTemperatureSensorRequest.CallOut);

                if (houseZone is null)
                {
                    houseZone = new HouseZone { Callout = createTemperatureSensorRequest.CallOut };

                    _context.HouseZones.Add(houseZone);
                    await _context.SaveChangesAsync();
                }

                long houseZoneId = houseZone.HouseZoneId;

                bool deviceExists = await _context.Devices.AnyAsync(d =>
                    d.HouseZoneId == houseZoneId &&
                    d.DeviceName == createTemperatureSensorRequest.DeviceName &&
                    d.DeviceType == createTemperatureSensorRequest.DeviceType);

                if (deviceExists)
                {
                    createTemperatureSensorResponse.ResponseCode = ResponseCodes.CONFLICT;
                    createTemperatureSensorResponse.IsSuccess = false;
                    createTemperatureSensorResponse.Message = $"the device with macAddress {createTemperatureSensorRequest.MacAddress} and device name {createTemperatureSensorRequest.DeviceName} already exists";
                }
                else
                {
                    Device device = new Device
                    {
                        HouseZoneId = houseZoneId,
                        DeviceName = createTemperatureSensorRequest.DeviceName,
                        DeviceType = createTemperatureSensorRequest.DeviceType,
                        Model = createTemperatureSensorRequest.Model,
                        Manufacturer = createTemperatureSensorRequest.Manufacturer,
                        MacAddress = createTemperatureSensorRequest.MacAddress,
                    };

                    _context.Devices.Add(device);
                    await _context.SaveChangesAsync();

                    TemperatureSensor temperatureSensor = new TemperatureSensor
                    {
                        DeviceId = device.DeviceId,
                        Temperature = createTemperatureSensorRequest.Temperature,
                        AdcVoltage = createTemperatureSensorRequest.AdcVoltage,
                        MeasureAt = createTemperatureSensorRequest.MeasureAt,
                    };

                    _context.TemperatureSensors.Add(temperatureSensor);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    createTemperatureSensorResponse.ResponseCode = ResponseCodes.CREATED;
                    createTemperatureSensorResponse.IsSuccess = true;
                    createTemperatureSensorResponse.Message = $"Temperature Sensor with the name {createTemperatureSensorRequest.DeviceName} created successfully.";
                }
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();

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
                HouseZone? houseZone = await _context.HouseZones.FirstOrDefaultAsync(h => h.Callout == patchTemperatureSensorRequest.CallOut);

                if (houseZone is null)
                {
                    patchTemperatureSensorResponse.ResponseCode = ResponseCodes.NOT_FOUND;
                    patchTemperatureSensorResponse.IsSuccess = false;
                    patchTemperatureSensorResponse.Message = $"House zone {patchTemperatureSensorRequest.CallOut} not found";
                }
                else
                {
                    long houseZoneId = houseZone.HouseZoneId;

                    Device? device = await _context.Devices.FirstOrDefaultAsync(d =>
                        d.HouseZoneId == houseZoneId &&
                        d.DeviceName == patchTemperatureSensorRequest.DeviceName &&
                        d.DeviceType == patchTemperatureSensorRequest.DeviceType);

                    if (device is null)
                    {
                        patchTemperatureSensorResponse.ResponseCode = ResponseCodes.NOT_FOUND;
                        patchTemperatureSensorResponse.IsSuccess = false;
                        patchTemperatureSensorResponse.Message = $"Device {patchTemperatureSensorRequest.DeviceName} not found";
                    }
                    else
                    {
                        long deviceId = device.DeviceId;
                        TemperatureSensor? temperatureSensor = await _context.TemperatureSensors.FirstOrDefaultAsync(t => t.DeviceId == deviceId);

                        if (temperatureSensor is null)
                        {
                            patchTemperatureSensorResponse.ResponseCode = ResponseCodes.NOT_FOUND;
                            patchTemperatureSensorResponse.IsSuccess = false;
                            patchTemperatureSensorResponse.Message = $"No temperature sensor found with the name {device.DeviceName}";
                        }
                        else
                        {
                            if (!GeneralUtils.IsNullOrEmpty(patchTemperatureSensorRequest.Model))
                                device.Model = patchTemperatureSensorRequest.Model;

                            if (!GeneralUtils.IsNullOrEmpty(patchTemperatureSensorRequest.MacAddress))
                                device.MacAddress = patchTemperatureSensorRequest.MacAddress;

                            // Las lecturas son opcionales: solo se pisa lo que llega.
                            if (patchTemperatureSensorRequest.Temperature is not null)
                                temperatureSensor.Temperature = patchTemperatureSensorRequest.Temperature.Value;

                            if (patchTemperatureSensorRequest.AdcVoltage is not null)
                                temperatureSensor.AdcVoltage = patchTemperatureSensorRequest.AdcVoltage.Value;

                            if (patchTemperatureSensorRequest.MeasureAt is not null)
                                temperatureSensor.MeasureAt = patchTemperatureSensorRequest.MeasureAt.Value;

                            await _context.SaveChangesAsync();

                            patchTemperatureSensorResponse.ResponseCode = ResponseCodes.OK;
                            patchTemperatureSensorResponse.IsSuccess = true;
                            patchTemperatureSensorResponse.Message = $"Temperature Sensor with the name {device.DeviceName} updated successfully.";
                        }
                    }
                }
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
