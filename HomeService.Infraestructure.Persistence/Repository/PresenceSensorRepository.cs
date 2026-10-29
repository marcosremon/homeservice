using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.CreatePresenceSensor;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.GetPresenceSensorsStatus;
using HomeService.Application.DataTransferObject.HomeAutomation.Sensor.PresenceSensor.PatchPresenceSensorData;
using HomeService.Application.Interface.Repository;
using HomeService.Domain.Model.Entity;
using HomeService.Infraestructure.Persistence.Context;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.Wrapper.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
                // Una sola transaccion: zona, dispositivo y sensor se guardan juntos o nada.
                // Los SaveChanges intermedios son para obtener los id generados.
                await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();

                HouseZone? houseZone = await _context.HouseZones.FirstOrDefaultAsync(h => h.Callout == createPresenceSensorRequest.CallOut);

                if (houseZone is null)
                {
                    houseZone = new HouseZone { Callout = createPresenceSensorRequest.CallOut };

                    _context.HouseZones.Add(houseZone);
                    await _context.SaveChangesAsync();
                }

                long houseZoneId = houseZone.HouseZoneId;

                Device? device = await _context.Devices.FirstOrDefaultAsync(d =>
                    d.HouseZoneId == houseZoneId &&
                    d.DeviceName == createPresenceSensorRequest.DeviceName &&
                    d.DeviceType == createPresenceSensorRequest.DeviceType);

                if (device is null)
                {
                    device = new Device
                    {
                        HouseZoneId = houseZoneId,
                        DeviceName = createPresenceSensorRequest.DeviceName,
                        DeviceType = createPresenceSensorRequest.DeviceType,
                        Model = createPresenceSensorRequest.Model,
                        Manufacturer = createPresenceSensorRequest.Manufacturer,
                        MacAddress = createPresenceSensorRequest.MacAddress,
                    };

                    _context.Devices.Add(device);
                    await _context.SaveChangesAsync();
                }

                PresenceSensor presenceSensor = new PresenceSensor
                {
                    DeviceId = device.DeviceId,
                    Ts = createPresenceSensorRequest.Ts,
                    Presence = createPresenceSensorRequest.Presence,
                    DistanceCm = createPresenceSensorRequest.DistanceCm,
                    Motion = createPresenceSensorRequest.Motion,
                    LastDetectedPresence = createPresenceSensorRequest.Presence ? GeneralUtils.UtcNow() : DateTime.MinValue,
                };

                _context.PresenceSensors.Add(presenceSensor);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                createPresenceSensorResponse.ResponseCode = ResponseCodes.CREATED;
                createPresenceSensorResponse.IsSuccess = true;
                createPresenceSensorResponse.Message = $"Presence Sensor with the name {createPresenceSensorRequest.DeviceName} created successfully.";
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();

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
                HouseZone? houseZone = await _context.HouseZones.FirstOrDefaultAsync(h => h.Callout == patchPresenceSensorDataRequest.CallOut);

                if (houseZone is null)
                {
                    patchPresenceSensorDataResponse.ResponseCode = ResponseCodes.NOT_FOUND;
                    patchPresenceSensorDataResponse.IsSuccess = false;
                    patchPresenceSensorDataResponse.Message = $"House zone {patchPresenceSensorDataRequest.CallOut} not found";
                }
                else
                {
                    long houseZoneId = houseZone.HouseZoneId;

                    Device? device = await _context.Devices.FirstOrDefaultAsync(d =>
                        d.HouseZoneId == houseZoneId &&
                        d.DeviceName == patchPresenceSensorDataRequest.DeviceName &&
                        d.DeviceType == patchPresenceSensorDataRequest.DeviceType);

                    if (device is null)
                    {
                        patchPresenceSensorDataResponse.ResponseCode = ResponseCodes.NOT_FOUND;
                        patchPresenceSensorDataResponse.IsSuccess = false;
                        patchPresenceSensorDataResponse.Message = $"Device {patchPresenceSensorDataRequest.DeviceName} not found";
                    }
                    else
                    {
                        long deviceId = device.DeviceId;
                        PresenceSensor? presenceSensor = await _context.PresenceSensors.FirstOrDefaultAsync(p => p.DeviceId == deviceId);

                        if (presenceSensor is null)
                        {
                            patchPresenceSensorDataResponse.ResponseCode = ResponseCodes.NOT_FOUND;
                            patchPresenceSensorDataResponse.IsSuccess = false;
                            patchPresenceSensorDataResponse.Message = $"No presence sensor found with the name {device.DeviceName}";
                        }
                        else
                        {
                            if (!GeneralUtils.IsNullOrEmpty(patchPresenceSensorDataRequest.Model))
                                device.Model = patchPresenceSensorDataRequest.Model;

                            if (!GeneralUtils.IsNullOrEmpty(patchPresenceSensorDataRequest.Manufacturer))
                                device.Manufacturer = patchPresenceSensorDataRequest.Manufacturer;

                            if (!GeneralUtils.IsNullOrEmpty(patchPresenceSensorDataRequest.MacAddress))
                                device.MacAddress = patchPresenceSensorDataRequest.MacAddress;

                            presenceSensor.Ts = patchPresenceSensorDataRequest.Ts;
                            presenceSensor.Presence = patchPresenceSensorDataRequest.Presence;
                            presenceSensor.DistanceCm = patchPresenceSensorDataRequest.DistanceCm;
                            presenceSensor.Motion = patchPresenceSensorDataRequest.Motion;

                            if (patchPresenceSensorDataRequest.Presence)
                                presenceSensor.LastDetectedPresence = GeneralUtils.UtcNow();

                            await _context.SaveChangesAsync();

                            patchPresenceSensorDataResponse.ResponseCode = ResponseCodes.OK;
                            patchPresenceSensorDataResponse.IsSuccess = true;
                            patchPresenceSensorDataResponse.Message = $"Presence Sensor with the name {device.DeviceName} updated successfully.";
                        }
                    }
                }
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
                if (!await _context.PresenceSensors.AnyAsync())
                {
                    getPresenceSensorsStatusResponse.ResponseCode = ResponseCodes.NOT_FOUND;
                    getPresenceSensorsStatusResponse.IsSuccess = false;
                    getPresenceSensorsStatusResponse.Message = "No presence sensors found.";
                }
                else
                {
                    Roomba? roomba = await _context.Roombas.FirstOrDefaultAsync();

                    if (roomba is null)
                    {
                        getPresenceSensorsStatusResponse.ResponseCode = ResponseCodes.NOT_FOUND;
                        getPresenceSensorsStatusResponse.IsSuccess = false;
                        getPresenceSensorsStatusResponse.Message = "No roomba found.";
                    }
                    else
                    {
                        getPresenceSensorsStatusResponse.IsHouseEmpty = !await _context.PresenceSensors.AnyAsync(p => p.Presence);
                        getPresenceSensorsStatusResponse.LastRoombaActivation = roomba.LastRoombaActivation;
                        getPresenceSensorsStatusResponse.ResponseCode = ResponseCodes.OK;
                        getPresenceSensorsStatusResponse.IsSuccess = true;
                        getPresenceSensorsStatusResponse.Message = "Presence sensors status retrieved successfully.";
                    }
                }
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
