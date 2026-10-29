using HomeService.Application.DataTransferObject.Light;
using HomeService.Application.DataTransferObject.Light.GetLightByLocation;
using HomeService.Application.DataTransferObject.Light.PatchLightStatus;
using HomeService.Application.Interface.Repository;
using HomeService.Domain.Model.Entity;
using HomeService.Domain.Model.Enum;
using HomeService.Domain.Model.Enum.Light;
using HomeService.Infraestructure.Persistence.Context;
using HomeService.Transversal.Common.Utils;
using HomeService.Transversal.Wrapper.Base;
using Microsoft.EntityFrameworkCore;

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
                string location = getLightByLocationRequest.Location.ToString();
                Light? light = await _context.Lights.FirstOrDefaultAsync(l => l.Location == location);

                if (light is null)
                {
                    getLightByLocationResponse.ResponseCode = ResponseCodes.NOT_FOUND;
                    getLightByLocationResponse.IsSuccess = false;
                    getLightByLocationResponse.Message = $"light {location} not found";
                }
                else
                {
                    // Como en el python, el tipo de dispositivo de las luces se busca como "light".
                    string lightDeviceType = nameof(DeviceType.LIGHT).ToLowerInvariant();
                    long deviceId = light.DeviceId;

                    Device? device = await _context.Devices.FirstOrDefaultAsync(d => d.DeviceId == deviceId && d.DeviceType == lightDeviceType);

                    if (device is null)
                    {
                        getLightByLocationResponse.ResponseCode = ResponseCodes.NOT_FOUND;
                        getLightByLocationResponse.IsSuccess = false;
                        getLightByLocationResponse.Message = $"No light device found for {location}";
                    }
                    else
                    {
                        long houseZoneId = device.HouseZoneId;
                        HouseZone? houseZone = await _context.HouseZones.FirstOrDefaultAsync(h => h.HouseZoneId == houseZoneId);

                        getLightByLocationResponse.LightDto = new LightDto
                        {
                            Name = device.DeviceName,
                            Room = houseZone?.Callout ?? string.Empty,
                            Location = GeneralUtils.ParseEnum(light.Location, LightLocation.NONE),
                            MqttTopic = light.MqttTopic,
                            IsOn = light.IsOn,
                            Brightness = light.Brightness,
                            Color = light.Color,
                            ColorTemperature = light.ColorTemperature,
                            LastStatusChange = light.LastStatusChange,
                            IsOnline = light.IsOnline,
                            LastSeen = light.LastSeen,
                        };

                        getLightByLocationResponse.ResponseCode = ResponseCodes.OK;
                        getLightByLocationResponse.IsSuccess = true;
                        getLightByLocationResponse.Message = "light found";
                    }
                }
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
                string location = patchLightStatusRequest.LightLocation.ToString();
                Light? light = await _context.Lights.FirstOrDefaultAsync(l => l.Location == location);

                if (light is null)
                {
                    Console.WriteLine($"ERROR light {location} not found");
                }
                else if (light.IsOn != patchLightStatusRequest.IsOn)
                {
                    light.IsOn = patchLightStatusRequest.IsOn;
                    light.LastStatusChange = GeneralUtils.UtcNow();

                    await _context.SaveChangesAsync();

                    Console.WriteLine($"light {location} IsOn cambiado a {patchLightStatusRequest.IsOn}");
                }
            }
            catch (Exception ex)
            {
                _context.ChangeTracker.Clear();

                Console.WriteLine($"Unexpected error on LightRepository -> PatchLightStatus: {ex.Message}");
            }
        }
        #endregion
    }
}
