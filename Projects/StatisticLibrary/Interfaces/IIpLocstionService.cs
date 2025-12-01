using StatisticLibrary.Models.StatisticModels;

namespace StatisticLibrary.Interfaces;

public interface IIpLocationService
{
    Task<LocationInfo> GetLocationAsync(string ipAddress);
}
