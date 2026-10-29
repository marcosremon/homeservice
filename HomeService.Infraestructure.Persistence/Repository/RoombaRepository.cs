using HomeService.Application.DataTransferObject.Roomba.CreateRoomba;
using HomeService.Application.DataTransferObject.Roomba.PatchRoombaState;
using HomeService.Application.Interface.Repository;
using HomeService.Domain.Model.Entity;
using HomeService.Infraestructure.Persistence.Context;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.Wrapper.Base;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

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
                // Una sola transaccion: zona, dispositivo y roomba se guardan juntos o nada.
                // Los SaveChanges intermedios son para obtener los id generados.
                await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync();

                // Solo hay un roomba en casa: si ya existe uno, no se crea otro.
                if (await _context.Roombas.AnyAsync())
                {
                    createRoombaResponse.ResponseCode = ResponseCodes.ANY_ROOMBA_EXIST;
                    createRoombaResponse.IsSuccess = false;
                    createRoombaResponse.Message = "Any roomba exists on bbdd";
                }
                else
                {
                    HouseZone? houseZone = await _context.HouseZones.FirstOrDefaultAsync(h => h.Callout == createRoombaRequest.CallOut);

                    if (houseZone is null)
                    {
                        houseZone = new HouseZone { Callout = createRoombaRequest.CallOut };

                        _context.HouseZones.Add(houseZone);
                        await _context.SaveChangesAsync();
                    }

                    long houseZoneId = houseZone.HouseZoneId;

                    Device? device = await _context.Devices.FirstOrDefaultAsync(d =>
                        d.HouseZoneId == houseZoneId &&
                        d.DeviceName == createRoombaRequest.DeviceName &&
                        d.DeviceType == createRoombaRequest.DeviceType);

                    if (device is null)
                    {
                        device = new Device
                        {
                            HouseZoneId = houseZoneId,
                            DeviceName = createRoombaRequest.DeviceName,
                            DeviceType = createRoombaRequest.DeviceType,
                            Model = createRoombaRequest.Model,
                            Manufacturer = createRoombaRequest.Manufacturer,
                            MacAddress = createRoombaRequest.MacAddress,
                        };

                        _context.Devices.Add(device);
                        await _context.SaveChangesAsync();
                    }

                    _context.Roombas.Add(new Roomba { DeviceId = device.DeviceId });
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    createRoombaResponse.ResponseCode = ResponseCodes.CREATED;
                    createRoombaResponse.IsSuccess = true;
                    createRoombaResponse.Message = "roomba created";
                }
            }
            catch (Exception ex)
            {
                // Equivale al session.rollback(): descarta lo pendiente del contexto. La
                // transaccion se deshace sola al liberarse sin commit.
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
                Roomba? roomba = await _context.Roombas.FirstOrDefaultAsync();

                if (roomba is null)
                {
                    patchRoombaStateResponse.ResponseCode = ResponseCodes.NOT_FOUND;
                    patchRoombaStateResponse.IsSuccess = false;
                    patchRoombaStateResponse.Message = "roomba not found";
                }
                else
                {
                    // La hora del evento la pone el servidor al procesar la peticion, no la
                    // que mande el dispositivo.
                    DateTime eventTime = GeneralUtils.UtcNow();

                    if (patchRoombaStateRequest.IsActivation)
                    {
                        roomba.LastRoombaActivation = eventTime;
                        roomba.LastTarget = patchRoombaStateRequest.Target.ToString();
                    }

                    if (patchRoombaStateRequest.IsFinished)
                    {
                        roomba.LastRoombaEnd = eventTime;
                        roomba.LastCleanDurationMinutes = roomba.LastRoombaActivation == DateTime.MinValue
                            ? 0
                            : (int)(eventTime - roomba.LastRoombaActivation).TotalMinutes;
                    }

                    if (patchRoombaStateRequest.BatteryPercent > 0)
                        roomba.BatteryPercent = patchRoombaStateRequest.BatteryPercent;

                    if (!GeneralUtils.IsNullOrEmpty(patchRoombaStateRequest.PmapId))
                        roomba.PmapId = patchRoombaStateRequest.PmapId;

                    if (!GeneralUtils.IsNullOrEmpty(patchRoombaStateRequest.UserPmapvId))
                        roomba.UserPmapvId = patchRoombaStateRequest.UserPmapvId;

                    roomba.Phase = patchRoombaStateRequest.Phase.ToString();
                    roomba.BinFull = patchRoombaStateRequest.BinFull;
                    roomba.ErrorCode = patchRoombaStateRequest.ErrorCode;
                    roomba.ErrorMessage = patchRoombaStateRequest.ErrorMessage;
                    roomba.IsOnline = patchRoombaStateRequest.IsOnline;
                    roomba.LastSeen = eventTime;

                    await _context.SaveChangesAsync();

                    patchRoombaStateResponse.ResponseCode = ResponseCodes.OK;
                    patchRoombaStateResponse.IsSuccess = true;
                    patchRoombaStateResponse.Message = $"roomba state updated successfully (phase {roomba.Phase}, battery {roomba.BatteryPercent}%).";
                }
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
